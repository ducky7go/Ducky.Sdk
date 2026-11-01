// LocalizationService.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using SodaCraft.Localizations;
using UnityEngine;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// Central localization service.
/// - Holds an ordered list of sources. The first is the mod's own <c>Locales</c> folder; more can be registered.
/// - A lookup walks the language fallback chain (for example <c>zh-hant, zh, en</c>). For each language it checks the
///   sources in order, and returns the first non-empty text.
/// - Publishes the effective text of every key to the game through one <see cref="Apply"/> step, which removes only
///   the overrides this service set and sets only the ones that changed.
/// </summary>
internal sealed class LocalizationService
{
    private const string DefaultScope = "default";

    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    private readonly List<LocalizationSource> _sources = new();
    private readonly Dictionary<string, IReadOnlyList<string>> _configuredChains = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AppliedOverride> _applied = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _reportedCollisions = new(StringComparer.Ordinal);
    private readonly Action<string, string> _setOverrideText;
    private readonly Action<string> _removeOverrideText;
    private SystemLanguage _currentLanguage = SystemLanguage.English;
    private string _currentLanguageCode = "en";

    private LocalizationService()
        : this(ResolveDefaultModId(), ResolveLocalesPath(),
            LocalizationManager.SetOverrideText, key => LocalizationManager.RemoveOverrideText(key))
    {
        SelectLanguage(LocalizationManager.CurrentLanguage);
    }

    /// <summary>
    /// Seam for tests: the locales folder and the game's override-text calls are injected, so the loading and lookup
    /// logic runs without the game. Does not select a language; the production constructor does that.
    /// </summary>
    internal LocalizationService(string localesPath, Action<string, string> setOverrideText, Action<string> removeOverrideText)
        : this(new ModId(DefaultScope), localesPath, setOverrideText, removeOverrideText)
    {
    }

    private LocalizationService(ModId defaultModId, string localesPath, Action<string, string> setOverrideText,
        Action<string> removeOverrideText)
    {
        _setOverrideText = setOverrideText;
        _removeOverrideText = removeOverrideText;
        DefaultModId = defaultModId;
        _sources.Add(new LocalizationSource(new DirectoryLocalizationProvider(defaultModId, localesPath),
            new LocalizationSourceOptions()));
    }

    /// <summary>The ModId of the mod's own Locales folder (the first source).</summary>
    public ModId DefaultModId { get; }

    private static ModId ResolveDefaultModId()
    {
        try
        {
            return Helper.GetModId();
        }
        catch (Exception)
        {
            var name = Assembly.GetExecutingAssembly().GetName().Name;
            return new ModId(string.IsNullOrWhiteSpace(name) ? DefaultScope : name);
        }
    }

    private static string ResolveLocalesPath()
    {
        try
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? Application.dataPath;
            return Path.Combine(assemblyDirectory, "Locales");
        }
        catch (Exception)
        {
            return Path.Combine(Application.dataPath, "Locales");
        }
    }

    private void SelectLanguage(SystemLanguage language)
    {
        Log.Debug("[LocalizationService] Selecting language: " + language);
        _currentLanguage = language;
        _currentLanguageCode = GetLanguageCode(language);

        if (!_sources.Any(source => source.HasLanguage(_currentLanguageCode)))
        {
            Log.Warn(
                $"[LocalizationService] Localization not found for language: {_currentLanguageCode}, falling back to English");
        }

        Log.Debug($"[LocalizationService] Selected language: {_currentLanguageCode}");
        Apply();
    }

    public void SetLanguage(SystemLanguage language)
    {
        SelectLanguage(language);
    }

    /// <summary>
    /// Removes from the game every override this service set (hot reload disable). Texts of other mods stay.
    /// </summary>
    internal void ReleaseOverrideTexts()
    {
        foreach (var key in _applied.Keys.ToList())
        {
            _removeOverrideText(key);
        }

        _applied.Clear();
    }

    /// <summary>Registers a source. Registering the same directory under the same ModId again returns the existing one.</summary>
    public IDisposable Register(ILocalizationProvider provider, LocalizationSourceOptions? options)
    {
        if (provider is DirectoryLocalizationProvider directory)
        {
            var existing = _sources.FirstOrDefault(source =>
                source.Provider is DirectoryLocalizationProvider other &&
                source.ModId == provider.ModId &&
                string.Equals(Path.GetFullPath(other.Directory), Path.GetFullPath(directory.Directory), StringComparison.Ordinal));
            if (existing != null)
            {
                return new Registration(this, existing);
            }
        }

        var source = new LocalizationSource(provider, options ?? new LocalizationSourceOptions());
        _sources.Add(source);
        Log.Info($"[LocalizationService] Registered source '{provider.ModId.Value}' with {source.Languages.Count()} language(s)");
        Apply();
        return new Registration(this, source);
    }

    /// <summary>Removes a source and every override it owned, then re-applies what the other sources still supply.</summary>
    internal void Unregister(LocalizationSource source)
    {
        if (!_sources.Remove(source))
        {
            return;
        }

        Log.Info($"[LocalizationService] Unregistered source '{source.ModId.Value}'");
        Apply();
    }

    /// <summary>Re-reads the folders of the matching sources (all when <paramref name="modId"/> is null).</summary>
    public void Reindex(ModId? modId)
    {
        foreach (var source in _sources.Where(source => modId == null || source.ModId == modId.Value))
        {
            source.Reindex();
        }

        Apply();
    }

    /// <summary>Sets the fallback chain of a language. The chain is the language, the given codes, then <c>en</c>.</summary>
    public void ConfigureFallback(string language, IReadOnlyList<string> chain)
    {
        var code = LanguageCodes.Normalize(language);
        _configuredChains[code] = FallbackChain.Build(code, chain, Log.Warn);
        Apply();
    }

    public string Get(string key, string? fallback = null) => Get(_currentLanguage, key, fallback);

    public string Get(SystemLanguage language, string key, string? fallback = null)
    {
        var chain = ChainFor(GetLanguageCode(language));
        return TryResolve(chain, null, key, out var text) ? text : fallback ?? $"[{key}]";
    }

    /// <summary>Lookup restricted to the sources of one ModId (current language).</summary>
    public string GetScoped(ModId modId, string key, string? fallback)
    {
        var chain = ChainFor(_currentLanguageCode);
        return TryResolve(chain, modId, key, out var text) ? text : fallback ?? $"[{key}]";
    }

    public bool TryGet(ModId? scope, string key, out string text)
        => TryResolve(ChainFor(_currentLanguageCode), scope, key, out text);

    /// <summary>
    /// Formats a key template with invariant culture. A bad template or argument list logs a warning and gives
    /// <c>false</c> with the unformatted template as the key, so the caller can return its missing-text result.
    /// </summary>
    public bool TryFormatKey(string template, object?[] args, out string key)
    {
        try
        {
            key = string.Format(CultureInfo.InvariantCulture, template, args);
            return true;
        }
        catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is IndexOutOfRangeException)
        {
            Log.Warn($"[LocalizationService] invalid key template '{template}' for {args.Length} argument(s): {ex.Message}");
            key = template;
            return false;
        }
    }

    /// <summary>
    /// Walks the chain: for each language, the sources in order (scoped to <paramref name="scope"/> when given).
    /// The first non-empty text wins.
    /// </summary>
    private bool TryResolve(IReadOnlyList<string> chain, ModId? scope, string key, out string text)
    {
        foreach (var language in chain)
        {
            foreach (var source in SourcesFor(scope))
            {
                if (source.TryGetText(language, key, out text))
                {
                    return true;
                }
            }
        }

        text = string.Empty;
        return false;
    }

    private IEnumerable<LocalizationSource> SourcesFor(ModId? scope)
        => scope == null ? _sources : _sources.Where(source => source.ModId == scope.Value);

    private IReadOnlyList<string> ChainFor(string code) => FallbackChain.For(code, _configuredChains);

    /// <summary>
    /// The one place that calls the game's override functions. It computes, for every key a publishing source supplies,
    /// the owner and text along the current chain, then removes the overrides that are no longer desired and sets the
    /// ones that are new or changed.
    /// </summary>
    private void Apply()
    {
        var chain = ChainFor(_currentLanguageCode);
        var desired = BuildDesiredOverrides(chain);

        foreach (var gameKey in _applied.Keys.Where(key => !desired.ContainsKey(key)).ToList())
        {
            _removeOverrideText(gameKey);
            _applied.Remove(gameKey);
        }

        foreach (var (gameKey, entry) in desired)
        {
            if (!_applied.TryGetValue(gameKey, out var previous) || previous.Text != entry.Text)
            {
                _setOverrideText(gameKey, entry.Text);
            }

            _applied[gameKey] = entry;
        }
    }

    private Dictionary<string, AppliedOverride> BuildDesiredOverrides(IReadOnlyList<string> chain)
    {
        var publishing = _sources.Where(source => source.Options.PublishToGame).ToList();

        // Every game key with the sources that supply it, in registration order.
        var candidates = new Dictionary<string, List<(LocalizationSource Source, string Key)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in publishing)
        {
            var prefix = source.Options.KeyPrefix ?? string.Empty;
            foreach (var key in source.KeysIn(chain).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var gameKey = prefix + key;
                if (!candidates.TryGetValue(gameKey, out var list))
                {
                    list = new List<(LocalizationSource, string)>();
                    candidates[gameKey] = list;
                }

                list.Add((source, key));
            }
        }

        var desired = new Dictionary<string, AppliedOverride>(StringComparer.OrdinalIgnoreCase);
        foreach (var (gameKey, list) in candidates)
        {
            foreach (var language in chain)
            {
                var hits = list
                    .Where(candidate => candidate.Source.TryGetText(language, candidate.Key, out _))
                    .Select(candidate => (candidate.Source, Text: candidate.Source.Load(language)[candidate.Key]))
                    .ToList();
                if (hits.Count == 0)
                {
                    continue;
                }

                var owner = hits[0];
                foreach (var other in hits.Skip(1))
                {
                    ReportCollision(gameKey, owner.Source.ModId, other.Source.ModId);
                }

                desired[gameKey] = new AppliedOverride(owner.Source.ModId, owner.Text);
                break;
            }
        }

        return desired;
    }

    private void ReportCollision(string gameKey, ModId owner, ModId other)
    {
        var pair = $"{gameKey}|{owner.Value}|{other.Value}";
        if (_reportedCollisions.Add(pair))
        {
            Log.Warn($"[LocalizationService] key '{gameKey}' is published by '{owner.Value}' and '{other.Value}'; '{owner.Value}' owns it");
        }
    }

    public static string GetLanguageCode(SystemLanguage language)
    {
        return language switch
        {
            SystemLanguage.Chinese => "zh",
            SystemLanguage.ChineseSimplified => "zh",
            SystemLanguage.ChineseTraditional => "zh-hant",
            SystemLanguage.English => "en",
            SystemLanguage.French => "fr",
            SystemLanguage.German => "de",
            SystemLanguage.Japanese => "ja",
            SystemLanguage.Korean => "ko",
            SystemLanguage.Portuguese => "pt",
            SystemLanguage.Russian => "ru",
            SystemLanguage.Spanish => "es",
            _ => "en"
        };
    }

    /// <summary>Handle returned by Register; disposing it unregisters the source once.</summary>
    private sealed class Registration : IDisposable
    {
        private readonly LocalizationService _service;
        private readonly LocalizationSource _source;
        private bool _disposed;

        public Registration(LocalizationService service, LocalizationSource source)
        {
            _service = service;
            _source = source;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _service.Unregister(_source);
        }
    }

    private readonly record struct AppliedOverride(ModId Owner, string Text);
}
