using System;
using System.Runtime.CompilerServices;
using FOW;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Ducky.Sdk.GameApis;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class FogOfWarPassExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetEffectEnabled(this FogOfWarPass target) =>
        target.GetField<FogOfWarPass, bool>("EffectEnabled");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FogOfWarPass SetEffectEnabled(this FogOfWarPass target, bool value) =>
        target.SetField("EffectEnabled", value);

    // Static field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FogOfWarPass GetStaticInstance() =>
        FieldExtensions.GetStaticField<FogOfWarPass, FogOfWarPass>("instance");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticInstance(FogOfWarPass value)
    {
        FieldExtensions.SetStaticField<FogOfWarPass, FogOfWarPass>("instance", value);
        return typeof(FogOfWarPass);
    }

}
