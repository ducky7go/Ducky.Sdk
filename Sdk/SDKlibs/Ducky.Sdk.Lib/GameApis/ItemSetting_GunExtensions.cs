using System;
using System.Runtime.CompilerServices;
using Duckov.Buffs;

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

    // Field 'muzzleFxPfb' uses object type (actual type: GameObject)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GetMuzzleFxPfb(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun>("muzzleFxPfb");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemSetting_Gun SetMuzzleFxPfb(this ItemSetting_Gun target, object value) =>
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

}
