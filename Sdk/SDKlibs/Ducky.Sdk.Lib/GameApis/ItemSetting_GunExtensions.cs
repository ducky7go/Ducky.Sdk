using Duckov.Buffs;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class ItemSetting_GunExtensions
{
    // Instance field accessors
    public static ADSAimMarker GetAdsAimMarker(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, ADSAimMarker>("adsAimMarker");

    public static void SetAdsAimMarker(this ItemSetting_Gun target, ADSAimMarker value) =>
        target.SetField("adsAimMarker", value);

    public static bool GetAutoReload(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, bool>("autoReload");

    public static void SetAutoReload(this ItemSetting_Gun target, bool value) =>
        target.SetField("autoReload", value);

    public static Buff GetBuff(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, Buff>("buff");

    public static void SetBuff(this ItemSetting_Gun target, Buff value) =>
        target.SetField("buff", value);

    public static Projectile GetBulletPfb(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, Projectile>("bulletPfb");

    public static void SetBulletPfb(this ItemSetting_Gun target, Projectile value) =>
        target.SetField("bulletPfb", value);

    public static ElementTypes GetElement(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, ElementTypes>("element");

    public static void SetElement(this ItemSetting_Gun target, ElementTypes value) =>
        target.SetField("element", value);

    // Field 'muzzleFxPfb' uses object type (actual type: GameObject)
    public static object GetMuzzleFxPfb(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun>("muzzleFxPfb");

    public static void SetMuzzleFxPfb(this ItemSetting_Gun target, object value) =>
        target.SetField("muzzleFxPfb", value);

    public static string GetReloadKey(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, string>("reloadKey");

    public static void SetReloadKey(this ItemSetting_Gun target, string value) =>
        target.SetField("reloadKey", value);

    public static ItemSetting_Gun.ReloadModes GetReloadMode(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, ItemSetting_Gun.ReloadModes>("reloadMode");

    public static void SetReloadMode(this ItemSetting_Gun target, ItemSetting_Gun.ReloadModes value) =>
        target.SetField("reloadMode", value);

    public static string GetShootKey(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, string>("shootKey");

    public static void SetShootKey(this ItemSetting_Gun target, string value) =>
        target.SetField("shootKey", value);

    public static ItemSetting_Gun.TriggerModes GetTriggerMode(this ItemSetting_Gun target) =>
        target.GetField<ItemSetting_Gun, ItemSetting_Gun.TriggerModes>("triggerMode");

    public static void SetTriggerMode(this ItemSetting_Gun target, ItemSetting_Gun.TriggerModes value) =>
        target.SetField("triggerMode", value);
}
