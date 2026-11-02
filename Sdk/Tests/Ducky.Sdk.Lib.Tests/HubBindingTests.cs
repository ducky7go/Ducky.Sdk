using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.ModProtocols;
using Shouldly;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>A stand-in for the hub object. <see cref="Alive"/> plays the part of Unity's null check.</summary>
internal sealed class FakeHubObject
{
    public bool Alive { get; set; } = true;
}

/// <summary>A hub proxy that records what the binding asked of it.</summary>
internal sealed class FakeHubProxy : IHubProxy
{
    public FakeHubProxy(bool isVirtual, FakeHubObject? hub = null)
    {
        IsVirtual = isVirtual;
        Hub = hub;
    }

    public FakeHubObject? Hub { get; }

    public bool IsVirtual { get; }

    public List<string> Registered { get; } = new();

    public List<string> Unregistered { get; } = new();

    public List<string> Notified { get; } = new();

    public int ReleaseCalls { get; private set; }

    public Dictionary<string, Func<string, string, string, UniTask>> Callbacks { get; } = new();

    public void RegisterClient(string modId, Func<string, string, string, UniTask> callback)
    {
        Registered.Add(modId);
        Callbacks[modId] = callback;
    }

    public void UnregisterClient(string modId)
    {
        Unregistered.Add(modId);
        Callbacks.Remove(modId);
    }

    public UniTask Notify(string fromModId, string toModId, string contentType, string body)
    {
        Notified.Add(body);
        return UniTask.CompletedTask;
    }

    public IReadOnlyList<string> GetModIds() => Callbacks.Keys.ToList();

    public void Release() => ReleaseCalls++;
}

/// <summary>
/// The binding over fake hub objects and fake proxies: the rules of task 7.1 (locate, rebind, generation, registration
/// once, no hub, virtual fallback) and the owner-aware unregister seen from the binding (task 7.2).
/// </summary>
[TestFixture]
public class HubBindingTests
{
    private const string ModId = "Acme.Client.http";

    private FakeHubObject? _current;
    private List<FakeHubProxy> _bound = null!;
    private HubBinding _binding = null!;

    [SetUp]
    public void SetUp()
    {
        _current = null;
        _bound = new List<FakeHubProxy>();
        _binding = Create(virtualFallback: false);
    }

    private HubBinding Create(bool virtualFallback)
    {
        return new HubBinding(
            locate: () => _current,
            isAlive: hub => ((FakeHubObject)hub).Alive,
            bindProxy: hub =>
            {
                var proxy = new FakeHubProxy(isVirtual: false, hub: (FakeHubObject)hub);
                _bound.Add(proxy);
                return proxy;
            },
            createVirtualProxy: virtualFallback
                ? () =>
                {
                    var proxy = new FakeHubProxy(isVirtual: true);
                    _bound.Add(proxy);
                    return proxy;
                }
                : null);
    }

    private static UniTask Handler(string from, string contentType, string body) => UniTask.CompletedTask;

    private static FakeHubObject NewHub(ref FakeHubObject? current)
    {
        current = new FakeHubObject();
        return current;
    }

    [Test]
    public void NoHubCreatesNoProxyAndSendsNothing()
    {
        _binding.GetModIds().ShouldBeEmpty();
        _binding.Register(ModId, Handler);
        _binding.Notify("a", "b", "text", "queued?").GetAwaiter().IsCompleted.ShouldBeTrue();

        _bound.ShouldBeEmpty();
        _binding.Generation.ShouldBe(0);
    }

    [Test]
    public void UnchangedHubRegistersEachClientOnceAndCreatesOneProxy()
    {
        NewHub(ref _current);
        _binding.Register("a", Handler);
        _binding.Register("b", Handler);
        _binding.GetModIds();
        _binding.Notify("a", "b", "text", "1");
        _binding.Notify("a", "b", "text", "2");
        _binding.Generation.ShouldBe(1);

        _bound.Count.ShouldBe(1);
        _bound[0].Registered.Count(id => id == "a").ShouldBe(1);
        _bound[0].Registered.Count(id => id == "b").ShouldBe(1);
        _bound[0].Notified.ShouldBe(new[] { "1", "2" });
    }

    [Test]
    public void ReplacedHubRegistersEveryTrackedClientOnceAndBumpsGeneration()
    {
        var first = NewHub(ref _current);
        _binding.Register("a", Handler);
        _binding.Register("b", Handler);
        var generationBefore = _binding.Generation;

        first.Alive = false;
        NewHub(ref _current);
        _binding.Notify("a", "b", "text", "after reload");

        _binding.Generation.ShouldBe(generationBefore + 1);
        _bound.Count.ShouldBe(2);
        _bound[1].Registered.OrderBy(id => id).ShouldBe(new[] { "a", "b" });
        _bound[1].Notified.ShouldBe(new[] { "after reload" });
    }

    [Test]
    public void SendAfterReplacementUsesTheNewProxyOnly()
    {
        var first = NewHub(ref _current);
        _binding.Register("a", Handler);
        first.Alive = false;
        NewHub(ref _current);

        _binding.Notify("a", "b", "text", "new");

        _bound[0].Notified.ShouldBeEmpty();
        _bound[1].Notified.ShouldBe(new[] { "new" });
    }

    [Test]
    public void DestroyedHubWithNoNewHubCreatesNothingUntilOneAppears()
    {
        var hub = NewHub(ref _current);
        _binding.Register("a", Handler);
        hub.Alive = false;
        _current = null;

        _binding.GetModIds().ShouldBeEmpty();
        _binding.Notify("a", "b", "text", "lost");
        _bound.Count.ShouldBe(1);
        _bound[0].Notified.ShouldBeEmpty();

        NewHub(ref _current);
        _binding.Notify("a", "b", "text", "found");

        _bound.Count.ShouldBe(2);
        _bound[1].Registered.ShouldContain("a");
        _bound[1].Notified.ShouldBe(new[] { "found" });
    }

    [Test]
    public void UnregisterRemovesTheRegistrationOfTheBoundHub()
    {
        NewHub(ref _current);
        _binding.Register("a", Handler);

        _binding.Unregister("a");

        _bound[0].Unregistered.ShouldBe(new[] { "a" });
        _binding.GetModIds().ShouldBeEmpty();
    }

    [Test]
    public void UnregisterAfterTheHubWasReplacedDoesNotTouchTheNewHub()
    {
        var first = NewHub(ref _current);
        _binding.Register("a", Handler);
        first.Alive = false;
        NewHub(ref _current);

        _binding.Unregister("a");

        _bound.Count.ShouldBe(1);
        _bound[0].Unregistered.ShouldBeEmpty();
    }

    [Test]
    public void NewRegistrationOnNewHubIsNotRemovedByTheOldBindingsTeardown()
    {
        // The new Core's binding registers the same id on the new hub; the old Core's binding is then released.
        var oldHub = NewHub(ref _current);
        var oldBinding = _binding;
        oldBinding.Register("a", Handler);
        oldHub.Alive = false;
        NewHub(ref _current);
        var newBinding = Create(virtualFallback: false);
        newBinding.Register("a", Handler);

        oldBinding.Release();

        _bound.Last().Unregistered.ShouldBeEmpty();
        _bound.Last().Callbacks.ContainsKey("a").ShouldBeTrue();
    }

    [Test]
    public void VirtualFallbackKeepsOneVirtualProxyWhileNoHubExists()
    {
        _binding = Create(virtualFallback: true);
        _binding.Register("a", Handler);
        _binding.Notify("a", "b", "text", "one");
        _binding.Notify("a", "b", "text", "two");

        _bound.Count.ShouldBe(1);
        _bound[0].IsVirtual.ShouldBeTrue();
        _bound[0].Registered.ShouldBe(new[] { "a" });
        _bound[0].Notified.ShouldBe(new[] { "one", "two" });
    }

    [Test]
    public void VirtualFallbackDoesNotBindASecondProxyWhenAHubAppears()
    {
        // The virtual proxy finds the hub through its own check and moves its clients and messages itself (task 7.6).
        _binding = Create(virtualFallback: true);
        _binding.Register("a", Handler);
        _binding.Notify("a", "b", "text", "held");
        NewHub(ref _current);

        _binding.Notify("a", "b", "text", "after");

        _bound.Count.ShouldBe(1);
        _bound[0].IsVirtual.ShouldBeTrue();
        _bound[0].Notified.ShouldBe(new[] { "held", "after" });
    }

    [Test]
    public void ReleaseUnregistersEveryClientOnceAndReleasesTheProxy()
    {
        NewHub(ref _current);
        _binding.Register("a", Handler);
        _binding.Register("b", Handler);

        _binding.Release();
        _binding.Release();

        _bound[0].Unregistered.OrderBy(id => id).ShouldBe(new[] { "a", "b" });
        _bound[0].ReleaseCalls.ShouldBe(1);
        _binding.GetModIds().ShouldBeEmpty();
    }
}

/// <summary>A plain hub with the owner-aware overload, bound through the real proxy's reflection path.</summary>
public sealed class OwnerAwareFakeHub
{
    public Dictionary<string, Func<string, string, string, UniTask>> Clients { get; } = new();

    public IReadOnlyList<string> ModIds => Clients.Keys.ToList();

    public void RegisterClient(string modId, Func<string, string, string, UniTask> callback) => Clients[modId] = callback;

    public void UnregisterClient(string modId) => Clients.Remove(modId);

    public void UnregisterClient(string modId, Func<string, string, string, UniTask> callback)
    {
        if (Clients.TryGetValue(modId, out var current) && ReferenceEquals(current, callback))
        {
            Clients.Remove(modId);
        }
    }

    public UniTask Notify(string fromModId, string toModId, string contentType, string body) => UniTask.CompletedTask;
}

/// <summary>A hub from an older SDK: only the id-only unregister exists.</summary>
public sealed class IdOnlyFakeHub
{
    public Dictionary<string, Func<string, string, string, UniTask>> Clients { get; } = new();

    public IReadOnlyList<string> ModIds => Clients.Keys.ToList();

    public void RegisterClient(string modId, Func<string, string, string, UniTask> callback) => Clients[modId] = callback;

    public void UnregisterClient(string modId) => Clients.Remove(modId);

    public UniTask Notify(string fromModId, string toModId, string contentType, string body) => UniTask.CompletedTask;
}

/// <summary>
/// Owner-aware unregister (task 7.2) through the real <see cref="ModHttpV1Proxy"/> bound to fake hubs, and the virtual
/// hand-over of pending messages (task 7.6).
/// </summary>
[TestFixture]
public class OwnerAwareUnregisterTests
{
    private const string ModId = "Acme.Client.http";

    private static readonly Func<string, string, string, UniTask> OldCallback = Old;
    private static readonly Func<string, string, string, UniTask> NewCallback = New;

    private static UniTask Old(string from, string contentType, string body) => UniTask.CompletedTask;

    private static UniTask New(string from, string contentType, string body) => UniTask.CompletedTask;

    [Test]
    public void NewerRegistrationSurvivesALateUnregisterOfTheOldOne()
    {
        var hub = new OwnerAwareFakeHub();
        var oldProxy = ModHttpV1Proxy.BindToHub(hub);
        var newProxy = ModHttpV1Proxy.BindToHub(hub);
        oldProxy.RegisterClient(ModId, OldCallback);
        newProxy.RegisterClient(ModId, NewCallback);

        oldProxy.UnregisterClient(ModId);

        hub.Clients.Keys.ShouldContain(ModId);
        hub.Clients[ModId].ShouldBeSameAs(NewCallback);
    }

    [Test]
    public void NormalUnregisterRemovesTheRegistration()
    {
        var hub = new OwnerAwareFakeHub();
        var proxy = ModHttpV1Proxy.BindToHub(hub);
        proxy.RegisterClient(ModId, OldCallback);

        proxy.UnregisterClient(ModId);

        hub.Clients.Keys.ShouldNotContain(ModId);
    }

    [Test]
    public void UnregisterOfAnIdThisProxyNeverRegisteredLeavesTheHubAlone()
    {
        var hub = new OwnerAwareFakeHub();
        var owner = ModHttpV1Proxy.BindToHub(hub);
        var other = ModHttpV1Proxy.BindToHub(hub);
        owner.RegisterClient(ModId, OldCallback);

        other.UnregisterClient(ModId);

        hub.Clients[ModId].ShouldBeSameAs(OldCallback);
    }

    [Test]
    public void HubWithoutTheOverloadFallsBackToIdOnlyUnregister()
    {
        var hub = new IdOnlyFakeHub();
        var proxy = ModHttpV1Proxy.BindToHub(hub);
        proxy.RegisterClient(ModId, OldCallback);

        Should.NotThrow(() => proxy.UnregisterClient(ModId));

        hub.Clients.Keys.ShouldNotContain(ModId);
    }

    [Test]
    public void VirtualProxyKeepsPendingMessagesAndDeliversThemOnceAfterTheUpgrade()
    {
        var virtualProxy = ModHttpV1Proxy.CreateVirtual();
        virtualProxy.Notify("a", "b", "text", "held-1");
        virtualProxy.Notify("a", "b", "text", "held-2");

        var delivered = new List<string>();
        virtualProxy.UpgradeToHub(new RecordingHub(delivered)).GetAwaiter().GetResult();
        virtualProxy.Notify("a", "b", "text", "after");

        delivered.ShouldBe(new[] { "held-1", "held-2", "after" });
    }

    /// <summary>A hub object that records the bodies it was asked to deliver.</summary>
    public sealed class RecordingHub
    {
        private readonly List<string> _delivered;

        public RecordingHub(List<string> delivered)
        {
            _delivered = delivered;
        }

        public IReadOnlyList<string> ModIds => Array.Empty<string>();

        public void RegisterClient(string modId, Func<string, string, string, UniTask> callback)
        {
        }

        public void UnregisterClient(string modId)
        {
        }

        public UniTask Notify(string fromModId, string toModId, string contentType, string body)
        {
            _delivered.Add(body);
            return UniTask.CompletedTask;
        }
    }
}
