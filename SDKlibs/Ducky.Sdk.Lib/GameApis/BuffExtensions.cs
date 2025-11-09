using System.Collections.Generic;
using Duckov.Buffs;
using ItemStatsSystem;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

public static class BuffExtensions
{
    // Instance field accessors
    // Field 'buffFxPfb' uses object type (actual type: GameObject)
    public static object GetBuffFxPfb(this Buff target) =>
        target.GetField<Buff>("buffFxPfb");

    public static void SetBuffFxPfb(this Buff target, object value) =>
        target.SetField("buffFxPfb", value);

    public static void SetCurrentLayers(this Buff target, int value) =>
        target.SetField("currentLayers", value);

    public static void SetDescription(this Buff target, string value) =>
        target.SetField("description", value);

    public static void SetDisplayName(this Buff target, string value) =>
        target.SetField("displayName", value);

    public static List<Effect> GetEffects(this Buff target) =>
        target.GetField<Buff, List<Effect>>("effects");

    public static void SetEffects(this Buff target, List<Effect> value) =>
        target.SetField("effects", value);

    public static void SetExclusiveTag(this Buff target, Buff.BuffExclusiveTags value) =>
        target.SetField("exclusiveTag", value);

    public static void SetExclusiveTagPriority(this Buff target, int value) =>
        target.SetField("exclusiveTagPriority", value);

    public static int GetFromWeaponID(this Buff target) =>
        target.GetField<Buff, int>("fromWeaponID");

    public static void SetFromWeaponID(this Buff target, int value) =>
        target.SetField("fromWeaponID", value);

    public static CharacterMainControl GetFromWho(this Buff target) =>
        target.GetField<Buff, CharacterMainControl>("fromWho");

    public static void SetFromWho(this Buff target, CharacterMainControl value) =>
        target.SetField("fromWho", value);

    public static void SetHide(this Buff target, bool value) =>
        target.SetField("hide", value);

    // Field 'icon' uses object type (actual type: Sprite)
    public static void SetIcon(this Buff target, object value) =>
        target.SetField("icon", value);

    public static int GetId(this Buff target) =>
        target.GetField<Buff, int>("id");

    public static void SetId(this Buff target, int value) =>
        target.SetField("id", value);

    public static void SetLimitedLifeTime(this Buff target, bool value) =>
        target.SetField("limitedLifeTime", value);

    public static void SetMaxLayers(this Buff target, int value) =>
        target.SetField("maxLayers", value);

    public static UnityEvent GetOnSetupEvent(this Buff target) =>
        target.GetField<Buff, UnityEvent>("OnSetupEvent");

    public static void SetOnSetupEvent(this Buff target, UnityEvent value) =>
        target.SetField("OnSetupEvent", value);

    public static void SetTotalLifeTime(this Buff target, float value) =>
        target.SetField("totalLifeTime", value);
}
