using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ducky.Sdk.Localizations;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>Records every log message so tests can assert warnings.</summary>
internal sealed class CapturingLog : ILog
{
    public List<string> Messages { get; } = new();

    public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception? exception = null, params object[] formatParameters)
    {
        // Level checks call Log with a null message function; they only need a yes.
        if (messageFunc != null)
        {
            Messages.Add(messageFunc());
        }

        return true;
    }

    public bool AnyContains(string text) => Messages.Any(message => message.Contains(text, StringComparison.Ordinal));
}

/// <summary>In-memory provider for source tests. Counts how often each language is loaded.</summary>
internal sealed class MemoryProvider : ILocalizationProvider
{
    private readonly Dictionary<string, Dictionary<string, string>> _languages;

    public MemoryProvider(string modId, Dictionary<string, Dictionary<string, string>> languages)
    {
        ModId = new ModId(modId);
        _languages = languages;
    }

    public ModId ModId { get; }

    public int LoadCount { get; private set; }

    public IReadOnlyCollection<string> Languages => _languages.Keys.ToList();

    public IReadOnlyDictionary<string, string> Load(string language)
    {
        LoadCount++;
        return _languages.TryGetValue(language, out var map) ? map : new Dictionary<string, string>();
    }
}

/// <summary>Temporary Locales folders and recorded game override calls for one test.</summary>
internal sealed class LocalizationFixture : IDisposable
{
    private readonly CapturingLog _log = new();
    private readonly ILog _previousLog = Log.Current;

    public LocalizationFixture()
    {
        Root = Path.Combine(AppContext.BaseDirectory, "loc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Log.Current = _log;
    }

    public string Root { get; }

    public List<(string Key, string Value)> Set { get; } = new();

    public List<string> Removed { get; } = new();

    public List<string> Messages => _log.Messages;

    public bool Logged(string text) => _log.AnyContains(text);

    public LocalizationService Create(string localesPath) =>
        new(localesPath, (key, value) => Set.Add((key, value)), key => Removed.Add(key));

    /// <summary>Creates a Locales folder with files given as relative path to content.</summary>
    public string Locales(string name, params (string Path, string Content)[] files)
    {
        var dir = Path.Combine(Root, name);
        foreach (var (path, content) in files)
        {
            var full = Path.Combine(dir, path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }

        Directory.CreateDirectory(dir);
        return dir;
    }

    public void Dispose()
    {
        Log.Current = _previousLog;
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, true);
        }
    }
}
