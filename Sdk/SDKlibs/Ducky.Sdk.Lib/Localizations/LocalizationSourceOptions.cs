namespace Ducky.Sdk.Localizations;

/// <summary>Options for a registered localization source.</summary>
public sealed class LocalizationSourceOptions
{
    /// <summary>
    /// Prefix added to every key this source publishes to the game (for example <c>dod.</c>). Null or empty publishes
    /// the raw keys, which is what the game's own <c>worldeffect.*</c> lookups expect.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>When false, the source is used for SDK lookups only and nothing is published to the game.</summary>
    public bool PublishToGame { get; set; } = true;
}
