namespace Ducky.Sdk.GameEvents;

/// <summary>What a pickup handler receives.</summary>
public readonly struct PickupInfo
{
    /// <summary>Creates the information of one pickup.</summary>
    public PickupInfo(InteractablePickup pickup, CharacterMainControl character)
    {
        Pickup = pickup;
        Character = character;
    }

    /// <summary>The picked-up object.</summary>
    public InteractablePickup Pickup { get; }

    /// <summary>The character that picked it up.</summary>
    public CharacterMainControl Character { get; }
}
