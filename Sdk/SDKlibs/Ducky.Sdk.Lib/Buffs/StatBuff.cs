using Duckov.Buffs;
using Ducky.Sdk.Logging;
using ItemStatsSystem;
using ItemStatsSystem.Stats;

namespace Ducky.Sdk.Buffs;

/// <summary>
/// A buff that changes stats of the character it sits on. Each change is a <see cref="Modifier"/> that the game sums
/// into the stat, so nothing is written over the stat's base value. The buff taking its modifiers back is all it takes
/// to undo the change, whether the buff times out, is removed or goes with the character.
/// </summary>
public abstract class StatBuff : Buff
{
    private readonly StatModifierLedger<Stat, Modifier> _applied = new();

    /// <summary>Called once each time the buff is set up on the character; call <see cref="Modify"/> from here.</summary>
    protected abstract void ApplyModifiers();

    /// <inheritdoc/>
    protected override void OnSetup()
    {
        // applying the same buff again takes back its earlier modifiers first, so none are left behind
        _applied.RemoveAll((stat, modifier) => stat.RemoveModifier(modifier));
        base.OnSetup();
        ApplyModifiers();
    }

    /// <summary>
    /// Adds a modifier to stat <paramref name="key"/> (the key the game uses, for example "ViewDistance").
    /// <see cref="ModifierType.Add"/> adds <paramref name="value"/>; <see cref="ModifierType.PercentageMultiply"/>
    /// multiplies the stat by 1 + <paramref name="value"/>, so 1 doubles it and -1 sets it to 0.
    /// The modifier's source is this buff.
    /// </summary>
    /// <returns>False when the character has no such stat; nothing is changed then.</returns>
    protected bool Modify(string key, ModifierType type, float value)
    {
        var item = Character != null ? Character.CharacterItem : null;
        var stat = item != null ? item.GetStat(key) : null;
        if (stat == null)
        {
            Log.Warn($"[{GetType().Name}] stat '{key}' not found on the character");
            return false;
        }

        var modifier = new Modifier(type, value, this);
        stat.AddModifier(modifier);
        _applied.Add(stat, modifier);
        return true;
    }

    /// <summary>The current value of a stat of the character, 0 when it has none.</summary>
    protected float StatValue(string key)
    {
        var item = Character != null ? Character.CharacterItem : null;
        return item != null ? item.GetStatValue(key.GetHashCode()) : 0f;
    }

    private void OnDestroy() => _applied.RemoveAll((stat, modifier) => stat.RemoveModifier(modifier));
}
