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
public static class SkillBaseExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetCoolDownTime(this SkillBase target) =>
        target.GetField<SkillBase, float>("coolDownTime");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkillBase SetCoolDownTime(this SkillBase target, float value) =>
        target.SetField("coolDownTime", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item GetFromItem(this SkillBase target) =>
        target.GetField<SkillBase, Item>("fromItem");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkillBase SetFromItem(this SkillBase target, Item value) =>
        target.SetField("fromItem", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetHasReleaseSound(this SkillBase target) =>
        target.GetField<SkillBase, bool>("hasReleaseSound");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkillBase SetHasReleaseSound(this SkillBase target, bool value) =>
        target.SetField("hasReleaseSound", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sprite GetIcon(this SkillBase target) =>
        target.GetField<SkillBase, Sprite>("icon");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkillBase SetIcon(this SkillBase target, Sprite value) =>
        target.SetField("icon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetOnReleaseSound(this SkillBase target) =>
        target.GetField<SkillBase, string>("onReleaseSound");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkillBase SetOnReleaseSound(this SkillBase target, string value) =>
        target.SetField("onReleaseSound", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action GetOnSkillReleasedEvent(this SkillBase target) =>
        target.GetField<SkillBase, Action>("OnSkillReleasedEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkillBase SetOnSkillReleasedEvent(this SkillBase target, Action value) =>
        target.SetField("OnSkillReleasedEvent", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkillBase SetSkillContext(this SkillBase target, SkillContext value) =>
        target.SetField("skillContext", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStaminaCost(this SkillBase target) =>
        target.GetField<SkillBase, float>("staminaCost");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkillBase SetStaminaCost(this SkillBase target, float value) =>
        target.SetField("staminaCost", value);

}
