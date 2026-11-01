using System.Collections.Generic;
using System.Linq;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// One registered source: a provider, its options, and a per-language cache of what the provider returned.
/// Keys are compared ignoring case by the provider's dictionaries.
/// </summary>
internal sealed class LocalizationSource
{
    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>(0);

    private readonly Dictionary<string, string> _providerCodes = new(); // canonical -> provider's own code
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _cache = new();

    public LocalizationSource(ILocalizationProvider provider, LocalizationSourceOptions options)
    {
        Provider = provider;
        Options = options;
        RebuildLanguages();
    }

    public ILocalizationProvider Provider { get; }

    public LocalizationSourceOptions Options { get; }

    public ModId ModId => Provider.ModId;

    /// <summary>Canonical language codes this source supplies.</summary>
    public IEnumerable<string> Languages => _providerCodes.Keys;

    public bool HasLanguage(string canonical) => _providerCodes.ContainsKey(canonical);

    /// <summary>The key/value pairs of one language, cached after the first load.</summary>
    public IReadOnlyDictionary<string, string> Load(string canonical)
    {
        if (_cache.TryGetValue(canonical, out var cached))
        {
            return cached;
        }

        if (!_providerCodes.TryGetValue(canonical, out var providerCode))
        {
            return Empty;
        }

        var loaded = Provider.Load(providerCode) ?? Empty;
        _cache[canonical] = loaded;
        return loaded;
    }

    /// <summary>The text for a key in one language, or false when it is missing or empty.</summary>
    public bool TryGetText(string canonical, string key, out string text)
    {
        if (Load(canonical).TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
        {
            text = value;
            return true;
        }

        text = string.Empty;
        return false;
    }

    /// <summary>All keys this source has in any of the given languages (first casing wins).</summary>
    public IEnumerable<string> KeysIn(IEnumerable<string> canonicalLanguages)
        => canonicalLanguages
            .Where(HasLanguage)
            .SelectMany(language => Load(language).Keys);

    /// <summary>Drops cached languages and rebuilds the language list from the provider.</summary>
    public void Reindex()
    {
        if (Provider is DirectoryLocalizationProvider directory)
        {
            directory.Reindex();
        }

        _cache.Clear();
        RebuildLanguages();
    }

    private void RebuildLanguages()
    {
        _providerCodes.Clear();
        foreach (var code in Provider.Languages)
        {
            var canonical = LanguageCodes.Normalize(code);
            if (canonical.Length > 0 && !_providerCodes.ContainsKey(canonical))
            {
                _providerCodes[canonical] = code;
            }
        }
    }
}
