namespace Ducky.Sdk.Control;

/// <summary>What a move or run rule knows about the character whose input is being set. Read-only.</summary>
public readonly struct ControlContext
{
    /// <summary>Creates the context for <paramref name="character"/>.</summary>
    public ControlContext(CharacterMainControl character, bool isPlayer)
    {
        Character = character;
        IsPlayer = isPlayer;
    }

    /// <summary>The character whose input is being set.</summary>
    public CharacterMainControl Character { get; }

    /// <summary>True when <see cref="Character"/> is the player (<c>CharacterMainControl.Main</c>).</summary>
    public bool IsPlayer { get; }
}
