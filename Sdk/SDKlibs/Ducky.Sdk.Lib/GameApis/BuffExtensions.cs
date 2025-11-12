using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Duckov.Buffs;
using ItemStatsSystem;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class BuffExtensions
{
    // Instance field accessors
    // Field 'buffFxPfb' uses object type (actual type: GameObject)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GetBuffFxPfb(this Buff target) =>
        target.GetField<Buff>("buffFxPfb");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetBuffFxPfb(this Buff target, object value) =>
        target.SetField("buffFxPfb", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetCurrentLayers(this Buff target, int value) =>
        target.SetField("currentLayers", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetDescription(this Buff target, string value) =>
        target.SetField("description", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetDisplayName(this Buff target, string value) =>
        target.SetField("displayName", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<Effect> GetEffects(this Buff target) =>
        target.GetField<Buff, List<Effect>>("effects");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetEffects(this Buff target, List<Effect> value) =>
        target.SetField("effects", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetExclusiveTag(this Buff target, Buff.BuffExclusiveTags value) =>
        target.SetField("exclusiveTag", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetExclusiveTagPriority(this Buff target, int value) =>
        target.SetField("exclusiveTagPriority", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFromWeaponID(this Buff target) =>
        target.GetField<Buff, int>("fromWeaponID");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetFromWeaponID(this Buff target, int value) =>
        target.SetField("fromWeaponID", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl GetFromWho(this Buff target) =>
        target.GetField<Buff, CharacterMainControl>("fromWho");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetFromWho(this Buff target, CharacterMainControl value) =>
        target.SetField("fromWho", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetHide(this Buff target, bool value) =>
        target.SetField("hide", value);

    // Field 'icon' uses object type (actual type: Sprite)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetIcon(this Buff target, object value) =>
        target.SetField("icon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetId(this Buff target) =>
        target.GetField<Buff, int>("id");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetId(this Buff target, int value) =>
        target.SetField("id", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetLimitedLifeTime(this Buff target, bool value) =>
        target.SetField("limitedLifeTime", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetMaxLayers(this Buff target, int value) =>
        target.SetField("maxLayers", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEvent GetOnSetupEvent(this Buff target) =>
        target.GetField<Buff, UnityEvent>("OnSetupEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetOnSetupEvent(this Buff target, UnityEvent value) =>
        target.SetField("OnSetupEvent", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff SetTotalLifeTime(this Buff target, float value) =>
        target.SetField("totalLifeTime", value);

}
