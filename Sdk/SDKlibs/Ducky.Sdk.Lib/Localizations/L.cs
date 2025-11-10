using SodaCraft.Localizations;
using UnityEngine;

namespace Ducky.Sdk.Localizations;

/// <summary>
/// Thin compatibility facade used by UI code.
/// Delegates localization work to <see cref="LocalizationService"/> which
/// implements the organized, folder-based loading similar to WorldEffectLocalizationManager.
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

    private LocalizationService Service => LocalizationService.Instance;

    private L()
    {
        LocalizationManager.OnSetLanguage += SetLanguage;
    }


    public void SetLanguage(SystemLanguage language)
    {
        Service.SetLanguage(language);
    }
}
