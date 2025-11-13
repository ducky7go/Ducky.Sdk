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
public static class ItemAgent_GunExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HandheldAnimationType GetHandAnimationType(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, HandheldAnimationType>("handAnimationType");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetHandAnimationType(this ItemAgent_Gun target, HandheldAnimationType value) =>
        target.SetField("handAnimationType", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HandheldSocketTypes GetHandheldSocket(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, HandheldSocketTypes>("handheldSocket");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetHandheldSocket(this ItemAgent_Gun target, HandheldSocketTypes value) =>
        target.SetField("handheldSocket", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetLoadedVisualObject(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, GameObject>("loadedVisualObject");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetLoadedVisualObject(this ItemAgent_Gun target, GameObject value) =>
        target.SetField("loadedVisualObject", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEvent GetOnInitializdEvent(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, UnityEvent>("OnInitializdEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetOnInitializdEvent(this ItemAgent_Gun target, UnityEvent value) =>
        target.SetField("OnInitializdEvent", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetSetActiveIfMainCharacter(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, GameObject>("setActiveIfMainCharacter");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetSetActiveIfMainCharacter(this ItemAgent_Gun target, GameObject value) =>
        target.SetField("setActiveIfMainCharacter", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ParticleSystem GetShellParticle(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, ParticleSystem>("shellParticle");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetShellParticle(this ItemAgent_Gun target, ParticleSystem value) =>
        target.SetField("shellParticle", value);
}
