using System;
using System.Reflection;
using Ducky.Sdk.Combat;
using Ducky.Sdk.Logging;

namespace Ducky.Sdk.Economy;

/// <summary>
/// The player's money. The game has two kinds: the bank deposit (<see cref="Money"/>, the game's
/// <c>EconomyManager.Money</c>) and the cash on hand (<see cref="Cash"/>, items of type <see cref="CashItemId"/>).
/// </summary>
/// <remarks>
/// The game's payment operation (<c>EconomyManager.Pay</c>) deducts the deposit first and takes cash only for the rest,
/// so it is never used here. <see cref="TryConsumeCash"/> takes cash items and never touches the deposit.
/// </remarks>
public static class Economy
{
    /// <summary>The item type id of cash (the physical money item).</summary>
    public const int CashItemId = 451;

    private static readonly HandlerSet<(long Old, long New)> MoneyChanges = new(
        onFirst: AttachMoneyChanged,
        onLast: DetachMoneyChanged,
        onFailure: ex => Log.Error(ex, "[Economy] A money-changed handler failed; other subscribers still run"));

    private static readonly Lazy<Func<int, long, bool>> ConsumeByType = new(FindConsumeByType);

    /// <summary>The bank deposit: the game's <c>EconomyManager.Money</c>. Not the cash on hand.</summary>
    public static long Money => Duckov.Economy.EconomyManager.Money;

    /// <summary>The cash on hand: the number of cash items (type <see cref="CashItemId"/>) the player holds.</summary>
    public static long Cash => Duckov.Economy.EconomyManager.Cash;

    /// <summary>
    /// Adds <paramref name="amount"/> to the bank deposit through the game's add operation. A negative amount reduces the
    /// deposit. Zero changes nothing.
    /// </summary>
    public static void AddMoney(long amount)
    {
        if (amount == 0)
        {
            return;
        }

        Duckov.Economy.EconomyManager.Add(amount);
    }

    /// <summary>
    /// Tries to consume <paramref name="amount"/> cash items. All or nothing: when the cash on hand is less than the amount it
    /// returns false and consumes nothing. Zero returns true and changes nothing. A negative amount throws.
    /// </summary>
    /// <remarks>
    /// This never calls the game's <c>EconomyManager.Pay</c>: <c>Pay</c> deducts the bank deposit first and uses cash only
    /// for the rest, so a mod that meant to take cash would take deposits.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The amount is negative.</exception>
    public static bool TryConsumeCash(long amount)
    {
        if (!CashPolicy.TryPlan(Cash, amount, out var toConsume))
        {
            return false;
        }

        if (toConsume == 0)
        {
            return true;
        }

        return ConsumeByType.Value(CashItemId, toConsume);
    }

    /// <summary>
    /// Runs <paramref name="handler"/> with the old and new bank deposit each time it changes. Dispose the returned handle to
    /// unsubscribe; disposing it more than once has no further effect.
    /// </summary>
    public static IDisposable OnMoneyChanged(Action<long, long> handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        return MoneyChanges.Add(args => handler(args.Old, args.New));
    }

    private static void AttachMoneyChanged() => Duckov.Economy.EconomyManager.OnMoneyChanged += RaiseMoneyChanged;

    private static void DetachMoneyChanged() => Duckov.Economy.EconomyManager.OnMoneyChanged -= RaiseMoneyChanged;

    private static void RaiseMoneyChanged(long oldMoney, long newMoney) => MoneyChanges.Raise((oldMoney, newMoney));

    // ItemUtilities.ConsumeItems(int, long) is internal in the game, so it is reached once by reflection.
    private static Func<int, long, bool> FindConsumeByType()
    {
        var method = typeof(ItemUtilities).GetMethod(
            "ConsumeItems",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(int), typeof(long) },
            null);
        if (method == null || method.ReturnType != typeof(bool))
        {
            throw new MissingMethodException(
                "ItemUtilities.ConsumeItems(int, long) was not found; the game's cash consumption cannot run.");
        }

        return (Func<int, long, bool>)Delegate.CreateDelegate(typeof(Func<int, long, bool>), method);
    }
}
