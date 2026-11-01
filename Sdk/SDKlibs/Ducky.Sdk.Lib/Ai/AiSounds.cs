using System;
using UnityEngine;

namespace Ducky.Sdk.Ai;

/// <summary>
/// Makes a sound that AI characters can hear, through the game's <c>AIMainBrain.MakeSound</c>.
/// </summary>
public static class AiSounds
{
    /// <summary>
    /// Makes a sound of <paramref name="type"/> at <paramref name="position"/> with <paramref name="radius"/>. When
    /// <paramref name="source"/> is given, the sound carries that character and its team; otherwise it carries the player's team
    /// and no character.
    /// </summary>
    /// <remarks>
    /// Radius is compared by each AI with the sound's distance after the radius is multiplied by that AI's <c>HearingAbility</c>,
    /// so the effective radius differs per listener. A sound from the listener's own team is ignored for unknown-noise and
    /// combat sounds, so a sound meant to attract enemies must not carry an enemy team. Main thread only.
    /// </remarks>
    /// <exception cref="ArgumentException">The radius is not a number.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The radius is negative or infinite.</exception>
    public static void Make(Vector3 position, float radius, SoundTypes type, CharacterMainControl? source = null)
    {
        AiSoundArgs.Validate(radius);

        var sound = new AISound
        {
            pos = position,
            radius = radius,
            soundType = type,
            fromTeam = source != null ? source.Team : Teams.player,
            fromCharacter = source,
        };
        AIMainBrain.MakeSound(sound);
    }
}
