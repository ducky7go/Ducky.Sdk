using System;
using System.Runtime.CompilerServices;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class Skill_GrenadeExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetBlastAngle(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, float>("blastAngle");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetBlastAngle(this Skill_Grenade target, float value) =>
        target.SetField("blastAngle", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBlastCount(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, int>("blastCount");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetBlastCount(this Skill_Grenade target, int value) =>
        target.SetField("blastCount", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetBlastDelayTimeSpace(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, float>("blastDelayTimeSpace");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetBlastDelayTimeSpace(this Skill_Grenade target, float value) =>
        target.SetField("blastDelayTimeSpace", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetCanControlCastDistance(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, bool>("canControlCastDistance");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetCanControlCastDistance(this Skill_Grenade target, bool value) =>
        target.SetField("canControlCastDistance", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetCanHurtSelf(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, bool>("canHurtSelf");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetCanHurtSelf(this Skill_Grenade target, bool value) =>
        target.SetField("canHurtSelf", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetCoolDownTime(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, float>("coolDownTime");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetCoolDownTime(this Skill_Grenade target, float value) =>
        target.SetField("coolDownTime", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetCreateExplosion(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, bool>("createExplosion");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetCreateExplosion(this Skill_Grenade target, bool value) =>
        target.SetField("createExplosion", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetCreatePickup(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, bool>("createPickup");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetCreatePickup(this Skill_Grenade target, bool value) =>
        target.SetField("createPickup", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DamageInfo GetDamageInfo(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, DamageInfo>("damageInfo");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetDamageInfo(this Skill_Grenade target, DamageInfo value) =>
        target.SetField("damageInfo", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetDelay(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, float>("delay");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetDelay(this Skill_Grenade target, float value) =>
        target.SetField("delay", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetDelayFromCollide(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, bool>("delayFromCollide");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetDelayFromCollide(this Skill_Grenade target, bool value) =>
        target.SetField("delayFromCollide", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetExplosionShakeStrength(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, float>("explosionShakeStrength");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetExplosionShakeStrength(this Skill_Grenade target, float value) =>
        target.SetField("explosionShakeStrength", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item GetFromItem(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, Item>("fromItem");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetFromItem(this Skill_Grenade target, Item value) =>
        target.SetField("fromItem", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Grenade GetGrenadePfb(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, Grenade>("grenadePfb");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetGrenadePfb(this Skill_Grenade target, Grenade value) =>
        target.SetField("grenadePfb", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetHasReleaseSound(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, bool>("hasReleaseSound");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetHasReleaseSound(this Skill_Grenade target, bool value) =>
        target.SetField("hasReleaseSound", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sprite GetIcon(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, Sprite>("icon");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetIcon(this Skill_Grenade target, Sprite value) =>
        target.SetField("icon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetIsLandmine(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, bool>("isLandmine");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetIsLandmine(this Skill_Grenade target, bool value) =>
        target.SetField("isLandmine", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetLandmineTriggerRange(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, float>("landmineTriggerRange");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetLandmineTriggerRange(this Skill_Grenade target, float value) =>
        target.SetField("landmineTriggerRange", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetOnReleaseSound(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, string>("onReleaseSound");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetOnReleaseSound(this Skill_Grenade target, string value) =>
        target.SetField("onReleaseSound", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action GetOnSkillReleasedEvent(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, Action>("OnSkillReleasedEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetOnSkillReleasedEvent(this Skill_Grenade target, Action value) =>
        target.SetField("OnSkillReleasedEvent", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetSkillContext(this Skill_Grenade target, SkillContext value) =>
        target.SetField("skillContext", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStaminaCost(this Skill_Grenade target) =>
        target.GetField<Skill_Grenade, float>("staminaCost");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skill_Grenade SetStaminaCost(this Skill_Grenade target, float value) =>
        target.SetField("staminaCost", value);

}
