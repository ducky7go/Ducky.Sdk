using System;

namespace Ducky.Sdk.Economy;

/// <summary>
/// The decision of whether an amount of cash can be consumed, with no game object. <see cref="Economy.TryConsumeCash"/>
/// asks this first, so the rule is tested without loading Unity.
/// </summary>
internal static class CashPolicy
{
    /// <summary>
    /// Plans consuming <paramref name="amount"/> from <paramref name="cash"/>. A negative amount throws. Zero is allowed and
    /// consumes nothing. An amount above the cash on hand is refused. Otherwise the whole amount is planned.
    /// </summary>
    /// <param name="cash">The cash on hand, zero or more.</param>
    /// <param name="amount">The amount to consume.</param>
    /// <param name="toConsume">The number of cash items to consume when the method returns true, otherwise zero.</param>
    /// <returns>True when the whole amount can be consumed.</returns>
    public static bool TryPlan(long cash, long amount, out long toConsume)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "The amount to consume must not be negative.");
        }

        if (amount == 0)
        {
            toConsume = 0;
            return true;
        }

        if (amount > cash)
        {
            toConsume = 0;
            return false;
        }

        toConsume = amount;
        return true;
    }
}
