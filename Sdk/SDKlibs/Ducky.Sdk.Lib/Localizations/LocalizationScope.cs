using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// Lookups that see only the sources registered under one <see cref="ModId"/>. Create with <c>L.Scope(modId)</c>.
/// </summary>
public readonly struct LocalizationScope
{
    private readonly ModId _modId;

    internal LocalizationScope(ModId modId)
    {
        _modId = modId;
    }

    public ModId ModId => _modId;

    /// <summary>The text of a key from this scope, or <c>[key]</c> when missing.</summary>
    public string Get(string key) => LocalizationService.Instance.GetScoped(_modId, key, null);

    /// <summary>The text of a key from this scope, or the fallback when missing.</summary>
    public string Get(string key, string fallback) => LocalizationService.Instance.GetScoped(_modId, key, fallback);

    /// <summary>Reports whether this scope has text for a key, without a placeholder when it does not.</summary>
    public bool TryGet(string key, out string text) => LocalizationService.Instance.TryGet(_modId, key, out text);

    /// <summary>Formats a key template and looks it up in this scope.</summary>
    public string Dynamic(string template, params object?[] args) => L.DynamicCore(LocalizationService.Instance, _modId, template, null, args);

    /// <summary>Formats a key template and looks it up in this scope, returning the fallback when missing.</summary>
    public string DynamicOr(string template, string fallback, params object?[] args) => L.DynamicCore(LocalizationService.Instance, _modId, template, fallback, args);

    /// <summary>Formats a key template and looks it up in this scope, reporting absence without a placeholder.</summary>
    public bool TryDynamic(string template, out string text, params object?[] args) => L.TryDynamicCore(LocalizationService.Instance, _modId, template, out text, args);
}
