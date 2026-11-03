using System.Globalization;
using System.Text;
using Duckov.Economy;
using Duckov.UI;
using ItemStatsSystem;

namespace Ducky.GameUiTabSample;

/// <summary>The player's attributes read from the game at one moment, and their brief text form.</summary>
internal sealed class PlayerAttributeSnapshot
{
    /// <summary>Current/max pairs and money: health, energy, water, stamina.</summary>
    internal List<(string Label, string Value)> Vitals { get; } = new();

    /// <summary>The stats that go into the brief text. Keys as the game uses them (see CharacterMainControl).</summary>
    private static readonly HashSet<string> BriefKeys = new()
    {
        "MaxHealth", "MaxWeight", "Stamina", "WalkSpeed", "RunSpeed", "InventoryCapacity",
        "GunDamageMultiplier", "GunCritRateGain", "ReloadSpeedGain", "MeleeDamageMultiplier",
    };

    /// <summary>
    /// Every stat of the character item, in the game's order, with the game's localized name and display format.
    /// <c>InBrief</c> marks the ones that the brief text includes.
    /// </summary>
    internal List<(string Label, string Value, bool InBrief)> Stats { get; } = new();

    /// <summary>Reads the live player. Returns null when there is none (title screen, scene loading).</summary>
    internal static PlayerAttributeSnapshot? Capture()
    {
        var main = CharacterMainControl.Main;
        if (main == null)
        {
            return null;
        }

        var snapshot = new PlayerAttributeSnapshot();
        if (main.Health != null)
        {
            snapshot.Vitals.Add(("HP", Pair(main.Health.CurrentHealth, main.Health.MaxHealth)));
        }

        snapshot.Vitals.Add(("Energy", Pair(main.CurrentEnergy, main.MaxEnergy)));
        snapshot.Vitals.Add(("Water", Pair(main.CurrentWater, main.MaxWater)));
        snapshot.Vitals.Add(("Stamina", Pair(main.CurrentStamina, main.MaxStamina)));
        snapshot.Vitals.Add(("Money", EconomyManager.Money.ToString("N0", CultureInfo.InvariantCulture)));

        var stats = main.CharacterItem != null ? main.CharacterItem.Stats : null;
        if (stats != null)
        {
            foreach (Stat stat in stats)
            {
                var format = StatInfoDatabase.Get(stat.Key).DisplayFormat;
                snapshot.Stats.Add((stat.DisplayName, stat.Value.ToString(format, CultureInfo.InvariantCulture), BriefKeys.Contains(stat.Key)));
            }
        }

        return snapshot;
    }

    /// <summary>
    /// The brief version for pasting: one line of vitals, then the key stats. The panel lists all of them.
    /// </summary>
    internal string ToBriefText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Duckov] Player brief");
        sb.AppendLine(string.Join("  ", Vitals.Select(v => v.Label + " " + v.Value)));
        var shown = Stats.Where(s => s.InBrief).Select(s => s.Label + " " + s.Value).ToList();
        if (shown.Count > 0)
        {
            sb.AppendLine(string.Join(", ", shown));
        }

        return sb.ToString().TrimEnd();
    }

    private static string Pair(float current, float max)
    {
        return current.ToString("0.#", CultureInfo.InvariantCulture) + "/" + max.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
