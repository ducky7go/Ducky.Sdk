using ItemStatsSystem.Stats;
using UnityEngine;

namespace Ducky.Sdk.Facade;

/// <summary>
/// The production <see cref="ICharacterOps{TCharacter}"/>: each member reads or writes the one game member written on it in the
/// interface, and nothing else. It holds no state.
/// </summary>
public sealed class CharacterOps : ICharacterOps<CharacterMainControl>
{
    /// <summary>The one production instance (the class has no state).</summary>
    public static readonly CharacterOps Instance = new();

    private CharacterOps()
    {
    }

    public bool Exists(CharacterMainControl? character) => character != null;

    public bool IsMainCharacter(CharacterMainControl character) => character.IsMainCharacter;

    public bool OnPlayerTeam(CharacterMainControl character) => character.IsMainCharacter || character.Team == Teams.player;

    public bool HasHealth(CharacterMainControl character) => character.Health != null;

    public bool IsDead(CharacterMainControl character) => character.Health != null && character.Health.IsDead;

    public bool HasItem(CharacterMainControl character) => character.CharacterItem != null;

    public string? PresetName(CharacterMainControl character) => character.characterPreset != null ? character.characterPreset.name : null;

    public float MaxHealth(CharacterMainControl character) => character.Health.MaxHealth;

    public float CurrentHealth(CharacterMainControl character) => character.Health.CurrentHealth;

    public void SetCurrentHealth(CharacterMainControl character, float value) => character.Health.CurrentHealth = value;

    public bool HasStat(CharacterMainControl character, string key) => character.CharacterItem.GetStat(key) != null;

    public float StatBaseValue(CharacterMainControl character, string key) => character.CharacterItem.GetStat(key).BaseValue;

    public float StatValue(CharacterMainControl character, string key) => character.CharacterItem.GetStat(key).Value;

    public void AddStatModifier(CharacterMainControl character, string key, StatModifierKind kind, float value, object source) =>
        character.CharacterItem.GetStat(key).AddModifier(new Modifier(
            kind == StatModifierKind.Add ? ModifierType.Add : ModifierType.PercentageAdd, value, source));

    public void RemoveStatModifiers(CharacterMainControl character, string key, object source) =>
        character.CharacterItem.GetStat(key)?.RemoveAllModifiersFromSource(source);

    public bool HasAi(CharacterMainControl character) => character.aiCharacterController != null;

    public float AiTraceDistance(CharacterMainControl character) => character.aiCharacterController.forceTracePlayerDistance;

    public void SetAiTraceDistance(CharacterMainControl character, float value) =>
        character.aiCharacterController.forceTracePlayerDistance = value;

    public WorldPoint PositionOf(CharacterMainControl character)
    {
        var position = character.transform.position;
        return new WorldPoint(position.x, position.y, position.z);
    }

    public float DistanceBetween(CharacterMainControl a, CharacterMainControl b) =>
        Vector3.Distance(a.transform.position, b.transform.position);

    public void HurtWithRealDamage(CharacterMainControl attacker, CharacterMainControl victim, float damage) =>
        victim.Health.Hurt(new DamageInfo(attacker)
        {
            damageValue = damage,
            damageType = DamageTypes.realDamage,
            isFromBuffOrEffect = true,
        });
}
