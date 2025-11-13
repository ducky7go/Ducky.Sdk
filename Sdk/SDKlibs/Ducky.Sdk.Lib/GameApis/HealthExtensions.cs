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
public static class HealthExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetAutoInit(this Health target) =>
        target.GetField<Health, bool>("autoInit");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetAutoInit(this Health target, bool value) =>
        target.SetField("autoInit", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetDeadDestroyDelay(this Health target) =>
        target.GetField<Health, float>("DeadDestroyDelay");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetDeadDestroyDelay(this Health target, float value) =>
        target.SetField("DeadDestroyDelay", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetDefaultMaxHealth(this Health target) =>
        target.GetField<Health, int>("defaultMaxHealth");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetDefaultMaxHealth(this Health target, int value) =>
        target.SetField("defaultMaxHealth", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetDestroyOnDead(this Health target) =>
        target.GetField<Health, bool>("DestroyOnDead");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetDestroyOnDead(this Health target, bool value) =>
        target.SetField("DestroyOnDead", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetHasSoul(this Health target) =>
        target.GetField<Health, bool>("hasSoul");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetHasSoul(this Health target, bool value) =>
        target.SetField("hasSoul", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetHealthBarHeight(this Health target) =>
        target.GetField<Health, float>("healthBarHeight");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetHealthBarHeight(this Health target, float value) =>
        target.SetField("healthBarHeight", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEvent<DamageInfo> GetOnDeadEvent(this Health target) =>
        target.GetField<Health, UnityEvent<DamageInfo>>("OnDeadEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetOnDeadEvent(this Health target, UnityEvent<DamageInfo> value) =>
        target.SetField("OnDeadEvent", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEvent<Health> GetOnHealthChange(this Health target) =>
        target.GetField<Health, UnityEvent<Health>>("OnHealthChange");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetOnHealthChange(this Health target, UnityEvent<Health> value) =>
        target.SetField("OnHealthChange", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEvent<DamageInfo> GetOnHurtEvent(this Health target) =>
        target.GetField<Health, UnityEvent<DamageInfo>>("OnHurtEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetOnHurtEvent(this Health target, UnityEvent<DamageInfo> value) =>
        target.SetField("OnHurtEvent", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEvent<Health> GetOnMaxHealthChange(this Health target) =>
        target.GetField<Health, UnityEvent<Health>>("OnMaxHealthChange");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetOnMaxHealthChange(this Health target, UnityEvent<Health> value) =>
        target.SetField("OnMaxHealthChange", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Teams GetTeam(this Health target) =>
        target.GetField<Health, Teams>("team");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Health SetTeam(this Health target, Teams value) =>
        target.SetField("team", value);

}
