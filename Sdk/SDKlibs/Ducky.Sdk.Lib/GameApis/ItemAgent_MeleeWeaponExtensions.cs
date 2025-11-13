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
public static class ItemAgent_MeleeWeaponExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HandheldAnimationType GetHandAnimationType(this ItemAgent_MeleeWeapon target) =>
        target.GetField<ItemAgent_MeleeWeapon, HandheldAnimationType>("handAnimationType");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_MeleeWeapon SetHandAnimationType(this ItemAgent_MeleeWeapon target,
        HandheldAnimationType value) =>
        target.SetField("handAnimationType", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HandheldSocketTypes GetHandheldSocket(this ItemAgent_MeleeWeapon target) =>
        target.GetField<ItemAgent_MeleeWeapon, HandheldSocketTypes>("handheldSocket");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_MeleeWeapon
        SetHandheldSocket(this ItemAgent_MeleeWeapon target, HandheldSocketTypes value) =>
        target.SetField("handheldSocket", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetHitFx(this ItemAgent_MeleeWeapon target) =>
        target.GetField<ItemAgent_MeleeWeapon, GameObject>("hitFx");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_MeleeWeapon SetHitFx(this ItemAgent_MeleeWeapon target, GameObject value) =>
        target.SetField("hitFx", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEvent GetOnInitializdEvent(this ItemAgent_MeleeWeapon target) =>
        target.GetField<ItemAgent_MeleeWeapon, UnityEvent>("OnInitializdEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_MeleeWeapon SetOnInitializdEvent(this ItemAgent_MeleeWeapon target, UnityEvent value) =>
        target.SetField("OnInitializdEvent", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetSetActiveIfMainCharacter(this ItemAgent_MeleeWeapon target) =>
        target.GetField<ItemAgent_MeleeWeapon, GameObject>("setActiveIfMainCharacter");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_MeleeWeapon
        SetSetActiveIfMainCharacter(this ItemAgent_MeleeWeapon target, GameObject value) =>
        target.SetField("setActiveIfMainCharacter", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetSlashFx(this ItemAgent_MeleeWeapon target) =>
        target.GetField<ItemAgent_MeleeWeapon, GameObject>("slashFx");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_MeleeWeapon SetSlashFx(this ItemAgent_MeleeWeapon target, GameObject value) =>
        target.SetField("slashFx", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetSlashFxDelayTime(this ItemAgent_MeleeWeapon target) =>
        target.GetField<ItemAgent_MeleeWeapon, float>("slashFxDelayTime");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_MeleeWeapon SetSlashFxDelayTime(this ItemAgent_MeleeWeapon target, float value) =>
        target.SetField("slashFxDelayTime", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_MeleeWeapon SetSoundKey(this ItemAgent_MeleeWeapon target, string value) =>
        target.SetField("soundKey", value);
}
