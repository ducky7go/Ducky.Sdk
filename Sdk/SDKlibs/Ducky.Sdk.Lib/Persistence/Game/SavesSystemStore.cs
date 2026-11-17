using Saves;

namespace Ducky.Sdk.Persistence;

/// <summary>The production <see cref="ISaveStore"/>: every call goes straight to the game's save system, nothing added.</summary>
public sealed class SavesSystemStore : ISaveStore
{
    /// <inheritdoc />
    public bool KeyExists(string key) => SavesSystem.KeyExisits(key);

    /// <inheritdoc />
    public string? LoadText(string key) => SavesSystem.Load<string>(key);

    /// <inheritdoc />
    public void SaveText(string key, string value) => SavesSystem.Save(key, value);
}
