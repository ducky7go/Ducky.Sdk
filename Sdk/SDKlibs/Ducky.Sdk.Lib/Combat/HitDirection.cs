namespace Ducky.Sdk.Combat;

/// <summary>Which way a hit went, decided from plain facts about the victim and the attacker. Touches no Unity object.</summary>
public static class HitDirection
{
    /// <summary>
    /// True when the player hit someone else. Damage with no attacker (an explosion, a trap) and damage to something that
    /// is not a character never counts.
    /// </summary>
    public static bool PlayerToEnemy(bool victimIsCharacter, bool hasAttacker, bool victimIsPlayer, bool attackerIsPlayer)
    {
        if (!victimIsCharacter || !hasAttacker)
        {
            return false;
        }

        return !victimIsPlayer && attackerIsPlayer;
    }
}
