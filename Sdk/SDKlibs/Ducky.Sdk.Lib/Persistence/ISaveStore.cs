namespace Ducky.Sdk.Persistence;

/// <summary>
/// The text store of the current save slot, as a mod's pure classes see it. The key names and the stored text are decided by the
/// callers; <c>SavesSystemStore</c> passes every call straight through to the game's save system, and a test uses an in-memory store.
/// </summary>
public interface ISaveStore
{
    /// <summary>True when a value is stored under this key.</summary>
    bool KeyExists(string key);

    /// <summary>The text stored under this key (may be null when the stored value is null).</summary>
    string? LoadText(string key);

    /// <summary>Stores the text under this key, replacing an earlier value.</summary>
    void SaveText(string key, string value);
}
