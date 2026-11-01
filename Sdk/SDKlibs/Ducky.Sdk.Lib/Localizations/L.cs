using System;
using Ducky.Sdk.Utils;
using SodaCraft.Localizations;
using UnityEngine;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// The public localization facade. Lookups use the current language and its fallback chain; missing text is
/// <c>[key]</c> unless a fallback is given. Sources are registered with <see cref="RegisterLocales"/> or
/// <see cref="Register"/>, and scoped with <see cref="Scope"/>.
/// </summary>
public partial class L
{
    private static L? _instance;
    public static L Instance => _instance ??= new L();

    /// <summary>
    /// Static accessor for simple usage from UI code.
    /// Delegates to LocalizationService directly and accepts only the key.
    /// </summary>
    public static string Get(string key)
    {
        return LocalizationService.Instance.Get(key);
    }

    /// <summary>The text of a key, or <paramref name="fallback"/> when no language has it.</summary>
    public static string Get(string key, string fallback)
    {
        return LocalizationService.Instance.Get(key, fallback);
    }

    /// <summary>Reports whether text exists for a key. Unlike <see cref="Get(string)"/>, no placeholder is produced.</summary>
    public static bool TryGet(string key, out string text)
    {
        return LocalizationService.Instance.TryGet(null, key, out text);
    }

    /// <summary>
    /// Formats a key template with invariant culture (for example <c>"worldeffect.{0}.name"</c>) and looks it up.
    /// A bad template or argument list does not throw; it logs a warning and gives <c>[template]</c>.
    /// </summary>
    public static string Dynamic(string template, params object?[] args)
    {
        return DynamicCore(LocalizationService.Instance, null, template, null, args);
    }

    /// <summary>As <see cref="Dynamic"/>, returning <paramref name="fallback"/> when the key or template is missing.</summary>
    public static string DynamicOr(string template, string fallback, params object?[] args)
    {
        return DynamicCore(LocalizationService.Instance, null, template, fallback, args);
    }

    /// <summary>As <see cref="Dynamic"/>, reporting absence without a placeholder.</summary>
    public static bool TryDynamic(string template, out string text, params object?[] args)
    {
        return TryDynamicCore(LocalizationService.Instance, null, template, out text, args);
    }

    /// <summary>Lookups restricted to the sources registered under <paramref name="modId"/>.</summary>
    public static LocalizationScope Scope(ModId modId)
    {
        return new LocalizationScope(modId);
    }

    /// <summary>
    /// Registers a mod's <c>Locales</c> folder as a source under <paramref name="modId"/>. Dispose the returned handle
    /// (on unload or hot reload) to remove the source and the game texts it owns. A missing folder logs a warning and
    /// registers an empty source.
    /// </summary>
    public static IDisposable RegisterLocales(ModId modId, string localesDirectory, LocalizationSourceOptions? options = null)
    {
        return LocalizationService.Instance.Register(new DirectoryLocalizationProvider(modId, localesDirectory), options);
    }

    /// <summary>Registers a custom provider as a source. Dispose the returned handle to unregister it.</summary>
    public static IDisposable Register(ILocalizationProvider provider, LocalizationSourceOptions? options = null)
    {
        return LocalizationService.Instance.Register(provider, options);
    }

    /// <summary>
    /// Sets the fallback chain of a language, for example <c>ConfigureFallback("zh-hant", "zh")</c>. The chain is the
    /// language, the given codes, then <c>en</c>. Invalid or repeated codes are ignored with a warning.
    /// </summary>
    public static void ConfigureFallback(string language, params string[] chain)
    {
        LocalizationService.Instance.ConfigureFallback(language, chain);
    }

    /// <summary>Re-reads the <c>Locales</c> folders of one ModId (or all sources when null) to pick up new files.</summary>
    public static void Reindex(ModId? modId = null)
    {
        LocalizationService.Instance.Reindex(modId);
    }

    /// <summary>Formats and looks up a key. Shared by <c>L</c>, <see cref="LocalizationScope"/> and the tests.</summary>
    internal static string DynamicCore(LocalizationService service, ModId? scope, string template, string? fallback, object?[] args)
    {
        if (!service.TryFormatKey(template, args, out var key))
        {
            return fallback ?? $"[{template}]";
        }

        return scope == null ? service.Get(key, fallback) : service.GetScoped(scope.Value, key, fallback);
    }

    /// <summary>Formats and looks up a key without a placeholder when it is missing.</summary>
    internal static bool TryDynamicCore(LocalizationService service, ModId? scope, string template, out string text, object?[] args)
    {
        if (!service.TryFormatKey(template, args, out var key))
        {
            text = string.Empty;
            return false;
        }

        return service.TryGet(scope, key, out text);
    }

    private LocalizationService Service => LocalizationService.Instance;

    private L()
    {
        Attach();
    }

    /// <summary>
    /// Subscribe to the game's language-changed event. Idempotent.
    /// </summary>
    internal void Attach()
    {
        LocalizationManager.OnSetLanguage -= SetLanguage;
        LocalizationManager.OnSetLanguage += SetLanguage;
    }

    /// <summary>
    /// Unsubscribe from the game's language-changed event (called when the mod is disabled). Idempotent.
    /// </summary>
    internal void Detach()
    {
        LocalizationManager.OnSetLanguage -= SetLanguage;
    }


    public void SetLanguage(SystemLanguage language)
    {
        Service.SetLanguage(language);
    }
}
