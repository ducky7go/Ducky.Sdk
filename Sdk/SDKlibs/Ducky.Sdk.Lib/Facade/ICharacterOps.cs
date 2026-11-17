namespace Ducky.Sdk.Facade;

/// <summary>
/// What a mod feature reads and writes on a game character, one operation per game member it touches (the game's character is
/// <typeparamref name="TCharacter"/>; production is <c>CharacterOps</c>). It is separate from
/// <see cref="IGameWorld{TCharacter}"/> because these are instance members of game objects, not static calls. Each member
/// says which game member it maps; none does more than that. A character is "gone" when the game has destroyed it
/// (<see cref="Exists"/>), which a plain null check cannot see, so a caller never compares a character with null itself.
/// Stats are addressed by their key (see <c>Ducky.Sdk.Buffs.StatKeys</c>).
/// </summary>
public interface ICharacterOps<TCharacter>
    where TCharacter : class
{
    /// <summary>False for null and for a destroyed character. Maps <c>character != null</c> with Unity's null check.</summary>
    bool Exists(TCharacter? character);

    /// <summary>Maps <c>character.IsMainCharacter</c>.</summary>
    bool IsMainCharacter(TCharacter character);

    /// <summary>True for the player and for everyone on its team. Maps <c>character.IsMainCharacter || character.Team == Teams.player</c>.</summary>
    bool OnPlayerTeam(TCharacter character);

    /// <summary>Maps <c>character.Health != null</c>.</summary>
    bool HasHealth(TCharacter character);

    /// <summary>True when it has health and the health is dead. Maps <c>character.Health != null &amp;&amp; character.Health.IsDead</c>.</summary>
    bool IsDead(TCharacter character);

    /// <summary>Maps <c>character.CharacterItem != null</c>.</summary>
    bool HasItem(TCharacter character);

    /// <summary>The name of the character's preset, or null without one. Maps <c>character.characterPreset != null ? character.characterPreset.name : null</c>.</summary>
    string? PresetName(TCharacter character);

    /// <summary>Maps <c>character.Health.MaxHealth</c>.</summary>
    float MaxHealth(TCharacter character);

    /// <summary>Maps <c>character.Health.CurrentHealth</c>.</summary>
    float CurrentHealth(TCharacter character);

    /// <summary>Maps the setter of <c>character.Health.CurrentHealth</c>.</summary>
    void SetCurrentHealth(TCharacter character, float value);

    /// <summary>Whether the character has the stat. Maps <c>character.CharacterItem.GetStat(key) != null</c>.</summary>
    bool HasStat(TCharacter character, string key);

    /// <summary>Maps <c>character.CharacterItem.GetStat(key).BaseValue</c>.</summary>
    float StatBaseValue(TCharacter character, string key);

    /// <summary>Maps <c>character.CharacterItem.GetStat(key).Value</c>.</summary>
    float StatValue(TCharacter character, string key);

    /// <summary>Maps <c>character.CharacterItem.GetStat(key).AddModifier(new Modifier(kind, value, source))</c>.</summary>
    void AddStatModifier(TCharacter character, string key, StatModifierKind kind, float value, object source);

    /// <summary>Maps <c>character.CharacterItem.GetStat(key).RemoveAllModifiersFromSource(source)</c>, and does nothing when the stat is missing (<c>GetStat(key)?.</c>).</summary>
    void RemoveStatModifiers(TCharacter character, string key, object source);

    /// <summary>Maps <c>character.aiCharacterController != null</c>.</summary>
    bool HasAi(TCharacter character);

    /// <summary>Maps <c>character.aiCharacterController.forceTracePlayerDistance</c>.</summary>
    float AiTraceDistance(TCharacter character);

    /// <summary>Maps the setter of <c>character.aiCharacterController.forceTracePlayerDistance</c>.</summary>
    void SetAiTraceDistance(TCharacter character, float value);

    /// <summary>Maps <c>character.transform.position</c>.</summary>
    WorldPoint PositionOf(TCharacter character);

    /// <summary>Maps <c>Vector3.Distance(a.transform.position, b.transform.position)</c>.</summary>
    float DistanceBetween(TCharacter a, TCharacter b);

    /// <summary>
    /// Real, non-scaled damage that is the attacker's own. Maps
    /// <c>victim.Health.Hurt(new DamageInfo(attacker) { damageValue = damage, damageType = DamageTypes.realDamage, isFromBuffOrEffect = true })</c>.
    /// </summary>
    void HurtWithRealDamage(TCharacter attacker, TCharacter victim, float damage);
}
