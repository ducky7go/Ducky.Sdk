using System;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class ExplosionManagerExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetFlashFxPfb(this ExplosionManager target) =>
        target.GetField<ExplosionManager, GameObject>("flashFxPfb");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExplosionManager SetFlashFxPfb(this ExplosionManager target, GameObject value) =>
        target.SetField("flashFxPfb", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetNormalFxPfb(this ExplosionManager target) =>
        target.GetField<ExplosionManager, GameObject>("normalFxPfb");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExplosionManager SetNormalFxPfb(this ExplosionManager target, GameObject value) =>
        target.SetField("normalFxPfb", value);

}
