using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts.DebugEvents;
using Ducky.Sdk.Contracts.HttpEndpoints;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Hot reload scenarios (tasks 9.2 to 9.4) on the loopback hub: a client Core is reloaded, the hub is replaced, or an old
/// Core tears down late. The host and the recorder stay running, as the consumer-side mods do in the game. Each scenario
/// checks what the host serves, or what the recorder file holds, so a duplicate or a loss shows up as a count.
/// </summary>
[TestFixture]
public class HotReloadScenarioTests
{
    private const string ModId = "Acme.Dash";
    private const int ReloadRepeats = 8;
    private static readonly string[] Paths = { "/Acme.Dash/status", "/Acme.Dash/items" };

    private HubLoopback _hub = null!;
    private HttpHostCore _host = null!;
    private List<HttpEndpointClient> _httpClients = null!;
    private StringWriter _file = null!;

    [SetUp]
    public void SetUp()
    {
        _hub = new HubLoopback();

        // The host (httphost) and the recorder run for the whole scenario, like the consumer-side mods do in the game.
        _host = new HttpHostCore(_hub.CreateOwner());
        _host.Start().ShouldBeTrue();
        _hub.Pump();

        var recorderOwner = _hub.CreateOwner();
        _file = new StringWriter();
        var writer = new DebugEventLineWriter(_file, () => _hub.SecondsSinceStart);
        var recorder = new DebugEventRecorder(
            recorderOwner,
            writer,
            message => _hub.Warnings.Add(message),
            message => _hub.Errors.Add(message));
        recorderOwner.Register(DebugEventProtocol.RecorderModId, recorder.HandleMessage);

        _httpClients = new List<HttpEndpointClient>();
    }

    [TearDown]
    public void TearDown()
    {
        _file.Dispose();
    }

    #region HTTP helpers

    /// <summary>Starts one client Core: its endpoints answer with <paramref name="label"/>, so a caller sees which Core answered.</summary>
    private HttpEndpointClient StartHttpClient(string label)
    {
        var client = new HttpEndpointClient(_hub.CreateOwner(), ModId);
        foreach (var path in Paths)
        {
            client.Map("GET", path, _ => HttpEndpointResponse.Text(200, label));
        }

        _httpClients.Add(client);
        return client;
    }

    /// <summary>The old Core's Release: withdraws its endpoints and unregisters its hub client.</summary>
    private void Withdraw(HttpEndpointClient client)
    {
        _httpClients.Remove(client);
        client.Shutdown();
        _hub.Pump();
    }

    /// <summary>Runs a few main-thread steps of every live client and of the host, delivering everything in between.</summary>
    private void Settle()
    {
        for (var round = 0; round < 3; round++)
        {
            _host.Tick();
            foreach (var client in _httpClients.ToList())
            {
                client.Tick();
            }

            _hub.Pump();
        }
    }

    private string Forward(string path)
    {
        var task = _host.ForwardAsync(new HttpEndpointRequest { Method = "GET", Path = path });
        _hub.Pump();
        task.Status.ShouldBe(UniTaskStatus.Succeeded);
        return task.GetAwaiter().GetResult().Body;
    }

    /// <summary>The host's route table holds every endpoint of the mod exactly once.</summary>
    private void ShouldListEachEndpointOnce()
    {
        _host.Routes.Count(r => r.OwnerModId == ModId).ShouldBe(Paths.Length);
        foreach (var path in Paths)
        {
            _host.Routes.Count(r => r.OwnerModId == ModId && r.Path == path).ShouldBe(1);
        }
    }

    #endregion

    #region Debug event helpers

    /// <summary>Starts one reporter Core. Like the sample, it reports one <c>core-enabled</c> event per enable.</summary>
    private DebugEventReporter StartReporter(string label)
    {
        var reporter = new DebugEventReporter(_hub.CreateOwner(), () => ModId, _ => { });

        // Register, negotiate with the recorder and become Ready. Reports are discarded before that.
        reporter.Tick();
        _hub.Pump();
        reporter.Report("core-enabled", label, null);
        return reporter;
    }

    private void Flush(DebugEventReporter reporter)
    {
        reporter.Tick();
        _hub.Pump();
    }

    /// <summary>The recorder file as (kind, detail) pairs, in file order.</summary>
    private List<(string Kind, string? Detail)> Events()
    {
        return _file.ToString()
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonLine.Parse(line.TrimEnd('\r')))
            .Select(line => ((string)line["kind"]!, (string?)line["detail"]))
            .ToList();
    }

    #endregion

    #region HTTP endpoints (task 9.2)

    [Test]
    public void ClientReloadKeepsEachEndpointOnceAndTheNewHandlerAnswers()
    {
        var first = StartHttpClient("core-1");
        Settle();
        ShouldListEachEndpointOnce();
        Forward(Paths[0]).ShouldBe("core-1");

        Withdraw(first);
        StartHttpClient("core-2");
        Settle();

        ShouldListEachEndpointOnce();
        Forward(Paths[0]).ShouldBe("core-2");
        Forward(Paths[1]).ShouldBe("core-2");
        _hub.Errors.ShouldBeEmpty();
    }

    [Test]
    public void HubReplacedClientRegistersAgainAndTheHostServes()
    {
        var client = StartHttpClient("core-1");
        Settle();

        _hub.ReplaceHub();
        Settle();

        client.State.ShouldBe(HttpEndpointClientState.Announced);
        ShouldListEachEndpointOnce();
        Forward(Paths[0]).ShouldBe("core-1");
        _hub.Errors.ShouldBeEmpty();
    }

    [Test]
    public void LateTeardownOfTheOldCoreKeepsTheNewRegistrationServing()
    {
        var old = StartHttpClient("core-1");
        Settle();

        // The new Core registers the same client id; then the old Core tears down late.
        var fresh = StartHttpClient("core-2");
        fresh.Tick();
        old.Shutdown();
        _hub.Pump();
        Settle();

        fresh.State.ShouldBe(HttpEndpointClientState.Announced);
        _hub.ModIds.ShouldContain(HttpEndpointProtocol.ClientId(ModId));
        ShouldListEachEndpointOnce();
        Forward(Paths[0]).ShouldBe("core-2");
    }

    #endregion

    #region Debug events (task 9.3)

    [Test]
    public void DebugClientReloadRecordsEachEventOnceInOrder()
    {
        var first = StartReporter("core-1");
        first.Report("burst", "1", null);
        first.Report("tick", "2", null);
        first.Shutdown();
        _hub.Pump();

        var second = StartReporter("core-2");
        second.Report("burst", "3", null);
        Flush(second);

        Events().ShouldBe(new (string, string?)[]
        {
            ("core-enabled", "core-1"),
            ("burst", "1"),
            ("tick", "2"),
            ("core-enabled", "core-2"),
            ("burst", "3"),
        });
        _hub.Errors.ShouldBeEmpty();
    }

    [Test]
    public void DebugHubReplacedReportsAfterTheRebindReachTheRecorderOnce()
    {
        var reporter = StartReporter("core-1");
        reporter.Report("before", "1", null);
        Flush(reporter);

        _hub.ReplaceHub();
        reporter.Report("after", "2", null);
        Flush(reporter);
        Flush(reporter);

        Events().ShouldBe(new (string, string?)[]
        {
            ("core-enabled", "core-1"),
            ("before", "1"),
            ("after", "2"),
        });
        _hub.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Repeated reloads (task 9.4)

    [Test]
    public void ReloadScenariosRepeatedEightTimesKeepEveryCountAtOne()
    {
        HttpEndpointClient? previous = null;
        for (var i = 1; i <= ReloadRepeats; i++)
        {
            var label = $"core-{i}";

            // HTTP: the old client is released, the new one announces, the host serves the new handler.
            if (previous != null)
            {
                Withdraw(previous);
            }

            previous = StartHttpClient(label);
            Settle();
            ShouldListEachEndpointOnce();
            Forward(Paths[0]).ShouldBe(label);

            // Debug events: the old reporter shuts down, the new one reports its enable and one burst.
            var reporter = StartReporter(label);
            reporter.Report("burst", label, null);
            reporter.Shutdown();
            _hub.Pump();
        }

        var events = Events();
        events.Count(e => e.Kind == "core-enabled").ShouldBe(ReloadRepeats);
        events.Count(e => e.Kind == "burst").ShouldBe(ReloadRepeats);
        for (var i = 1; i <= ReloadRepeats; i++)
        {
            var label = $"core-{i}";
            events.Count(e => e.Kind == "core-enabled" && e.Detail == label).ShouldBe(1);
            events.Count(e => e.Kind == "burst" && e.Detail == label).ShouldBe(1);
        }

        _hub.Errors.ShouldBeEmpty();
    }

    #endregion
}
