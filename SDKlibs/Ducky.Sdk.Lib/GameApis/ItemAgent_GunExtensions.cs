using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class ItemAgent_GunExtensions
{
    // Instance field accessors
    public static HandheldAnimationType GetHandAnimationType(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, HandheldAnimationType>("handAnimationType");

    public static void SetHandAnimationType(this ItemAgent_Gun target, HandheldAnimationType value) =>
        target.SetField("handAnimationType", value);

    public static HandheldSocketTypes GetHandheldSocket(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, HandheldSocketTypes>("handheldSocket");

    public static void SetHandheldSocket(this ItemAgent_Gun target, HandheldSocketTypes value) =>
        target.SetField("handheldSocket", value);

    // Field 'loadedVisualObject' uses object type (actual type: GameObject)
    public static object GetLoadedVisualObject(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun>("loadedVisualObject");

    public static void SetLoadedVisualObject(this ItemAgent_Gun target, object value) =>
        target.SetField("loadedVisualObject", value);

    public static UnityEvent GetOnInitializdEvent(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, UnityEvent>("OnInitializdEvent");

    public static void SetOnInitializdEvent(this ItemAgent_Gun target, UnityEvent value) =>
        target.SetField("OnInitializdEvent", value);

    // Field 'setActiveIfMainCharacter' uses object type (actual type: GameObject)
    public static object GetSetActiveIfMainCharacter(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun>("setActiveIfMainCharacter");

    public static void SetSetActiveIfMainCharacter(this ItemAgent_Gun target, object value) =>
        target.SetField("setActiveIfMainCharacter", value);

    public static ParticleSystem GetShellParticle(this ItemAgent_Gun target) =>
        target.GetField<ItemAgent_Gun, ParticleSystem>("shellParticle");

    public static void SetShellParticle(this ItemAgent_Gun target, ParticleSystem value) =>
        target.SetField("shellParticle", value);

}
