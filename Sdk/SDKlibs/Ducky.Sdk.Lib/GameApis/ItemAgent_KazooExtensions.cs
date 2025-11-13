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
public static class ItemAgent_KazooExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HandheldAnimationType GetHandAnimationType(this ItemAgent_Kazoo target) =>
        target.GetField<ItemAgent_Kazoo, HandheldAnimationType>("handAnimationType");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Kazoo SetHandAnimationType(this ItemAgent_Kazoo target, HandheldAnimationType value) =>
        target.SetField("handAnimationType", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HandheldSocketTypes GetHandheldSocket(this ItemAgent_Kazoo target) =>
        target.GetField<ItemAgent_Kazoo, HandheldSocketTypes>("handheldSocket");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Kazoo SetHandheldSocket(this ItemAgent_Kazoo target, HandheldSocketTypes value) =>
        target.SetField("handheldSocket", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetMaxScale(this ItemAgent_Kazoo target) =>
        target.GetField<ItemAgent_Kazoo, float>("maxScale");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Kazoo SetMaxScale(this ItemAgent_Kazoo target, float value) =>
        target.SetField("maxScale", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetMaxSoundRange(this ItemAgent_Kazoo target) =>
        target.GetField<ItemAgent_Kazoo, float>("maxSoundRange");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Kazoo SetMaxSoundRange(this ItemAgent_Kazoo target, float value) =>
        target.SetField("maxSoundRange", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEvent GetOnInitializdEvent(this ItemAgent_Kazoo target) =>
        target.GetField<ItemAgent_Kazoo, UnityEvent>("OnInitializdEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Kazoo SetOnInitializdEvent(this ItemAgent_Kazoo target, UnityEvent value) =>
        target.SetField("OnInitializdEvent", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ParticleSystem GetParticle(this ItemAgent_Kazoo target) =>
        target.GetField<ItemAgent_Kazoo, ParticleSystem>("particle");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Kazoo SetParticle(this ItemAgent_Kazoo target, ParticleSystem value) =>
        target.SetField("particle", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetSetActiveIfMainCharacter(this ItemAgent_Kazoo target) =>
        target.GetField<ItemAgent_Kazoo, GameObject>("setActiveIfMainCharacter");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Kazoo SetSetActiveIfMainCharacter(this ItemAgent_Kazoo target, GameObject value) =>
        target.SetField("setActiveIfMainCharacter", value);
}
