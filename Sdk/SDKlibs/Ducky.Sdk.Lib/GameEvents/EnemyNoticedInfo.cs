namespace Ducky.Sdk.GameEvents;

/// <summary>What a first-notice handler receives.</summary>
public readonly struct EnemyNoticedInfo
{
    /// <summary>Creates the information of one first notice.</summary>
    public EnemyNoticedInfo(CharacterMainControl enemy, DamageReceiver target)
    {
        Enemy = enemy;
        Target = target;
    }

    /// <summary>The enemy that noticed the player for the first time.</summary>
    public CharacterMainControl Enemy { get; }

    /// <summary>The receiver the enemy noticed, the player's main damage receiver.</summary>
    public DamageReceiver Target { get; }
}
