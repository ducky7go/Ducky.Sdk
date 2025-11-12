using System;
using System.Runtime.CompilerServices;
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

    // Field 'loadedVisualObject' uses object type (actual type: GameObject)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GetLoadedVisualObject(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun>("loadedVisualObject");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetLoadedVisualObject(this ItemAgent_Gun target, object value) =>
        target.SetField("loadedVisualObject", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnityEvent GetOnInitializdEvent(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, UnityEvent>("OnInitializdEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetOnInitializdEvent(this ItemAgent_Gun target, UnityEvent value) =>
        target.SetField("OnInitializdEvent", value);

    // Field 'setActiveIfMainCharacter' uses object type (actual type: GameObject)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GetSetActiveIfMainCharacter(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun>("setActiveIfMainCharacter");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetSetActiveIfMainCharacter(this ItemAgent_Gun target, object value) =>
        target.SetField("setActiveIfMainCharacter", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ParticleSystem GetShellParticle(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, ParticleSystem>("shellParticle");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgent_Gun SetShellParticle(this ItemAgent_Gun target, ParticleSystem value) =>
        target.SetField("shellParticle", value);
}
