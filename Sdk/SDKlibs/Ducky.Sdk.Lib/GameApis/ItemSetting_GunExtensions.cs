using System;
using System.Runtime.CompilerServices;
using Duckov.Buffs;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Ducky.Sdk.GameEvents;
using Ducky.Sdk.Logging;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class ItemSetting_GunExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ADSAimMarker GetAdsAimMarker(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, ADSAimMarker>("adsAimMarker");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetAdsAimMarker(this ItemSetting_Gun target, ADSAimMarker value) =>
        target.SetField("adsAimMarker", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAutoReload(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, bool>("autoReload");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetAutoReload(this ItemSetting_Gun target, bool value) =>
        target.SetField("autoReload", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff GetBuff(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, Buff>("buff");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetBuff(this ItemSetting_Gun target, Buff value) =>
        target.SetField("buff", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Projectile GetBulletPfb(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, Projectile>("bulletPfb");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetBulletPfb(this ItemSetting_Gun target, Projectile value) =>
        target.SetField("bulletPfb", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ElementTypes GetElement(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, ElementTypes>("element");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetElement(this ItemSetting_Gun target, ElementTypes value) =>
        target.SetField("element", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetMuzzleFxPfb(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, GameObject>("muzzleFxPfb");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetMuzzleFxPfb(this ItemSetting_Gun target, GameObject value) =>
        target.SetField("muzzleFxPfb", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetReloadKey(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, string>("reloadKey");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetReloadKey(this ItemSetting_Gun target, string value) =>
        target.SetField("reloadKey", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun.ReloadModes GetReloadMode(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, ItemSetting_Gun.ReloadModes>("reloadMode");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetReloadMode(this ItemSetting_Gun target, ItemSetting_Gun.ReloadModes value) =>
        target.SetField("reloadMode", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetShootKey(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, string>("shootKey");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetShootKey(this ItemSetting_Gun target, string value) =>
        target.SetField("shootKey", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun.TriggerModes GetTriggerMode(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, ItemSetting_Gun.TriggerModes>("triggerMode");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetTriggerMode(this ItemSetting_Gun target, ItemSetting_Gun.TriggerModes value) =>
        target.SetField("triggerMode", value);

    /// <summary>
    /// Reads the gun's bullet count (the game's private <c>bulletCount</c> member). Returns false when the member is not
    /// found, and logs that once.
    /// </summary>
    public static bool TryGetBulletCount(this ItemSetting_Gun target, out int count)
    {
        var accessor = BulletCount.Value;
        if (accessor == null)
        {
            LogBulletCountNotFoundOnce();
            count = 0;
            return false;
        }

        count = accessor.Get(target);
        return true;
    }

    /// <summary>
    /// Sets the gun's bullet count. Chains like the other setters. When the member is not found, or cannot be written,
    /// it logs once and does nothing.
    /// </summary>
    public static ItemSetting_Gun SetBulletCount(this ItemSetting_Gun target, int value)
    {
        var accessor = BulletCount.Value;
        if (accessor == null || !accessor.TrySet(target, value))
        {
            LogBulletCountNotFoundOnce();
            return target;
        }

        return target;
    }

    /// <summary>
    /// Runs <paramref name="handler"/> each time this gun hurts an enemy (the game's <c>onHurtEnemyEvent</c>). A handler
    /// that throws is logged and does not reach the game. Dispose the returned handle to unsubscribe; disposing it again
    /// does nothing.
    /// </summary>
    /// <remarks>
    /// <c>DamageInfo.crit</c> is -1 when this event fires, because the game rolls crits later, inside <c>Health.Hurt</c>.
    /// The subscription belongs to this gun object: when the held gun changes, subscribe to the new gun again.
    /// </remarks>
    public static IDisposable OnHurtEnemy(this ItemSetting_Gun target, Action<DamageReceiver, DamageInfo> handler)
    {
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        UnityAction<DamageReceiver, DamageInfo> listener = (receiver, info) =>
        {
            try
            {
                handler(receiver, info);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ItemSetting_Gun] An onHurtEnemy handler failed; the game is not affected");
            }
        };

        return EventSubscription.Create(
            listener,
            add: l => target.onHurtEnemyEvent.AddListener(l),
            remove: l => target.onHurtEnemyEvent.RemoveListener(l));
    }

    // Resolved on first use, never in a static constructor, so a failed lookup cannot poison this type.
    private static readonly Lazy<BulletCountAccessor?> BulletCount =
        new(() => BulletCountAccessor.Create(typeof(ItemSetting_Gun)));

    private static int _bulletCountMissingLogged;

    private static void LogBulletCountNotFoundOnce()
    {
        if (System.Threading.Interlocked.Exchange(ref _bulletCountMissingLogged, 1) == 0)
        {
            Ducky.Sdk.Logging.Log.Warn("ItemSetting_Gun '" + BulletCountAccessor.MemberName + "' member was not found or cannot be written; bullet count access does nothing.");
        }
    }
}
