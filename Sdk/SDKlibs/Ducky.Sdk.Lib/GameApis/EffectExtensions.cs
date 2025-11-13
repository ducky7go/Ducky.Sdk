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
public static class EffectExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Effect SetActions(this Effect target, List<EffectAction> value) =>
        target.SetField("actions", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Effect SetDescription(this Effect target, string value) =>
        target.SetField("description", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Effect SetDisplay(this Effect target, bool value) =>
        target.SetField("display", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Effect SetFilters(this Effect target, List<EffectFilter> value) =>
        target.SetField("filters", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Effect SetItem(this Effect target, Item value) =>
        target.SetField("item", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Effect SetTriggers(this Effect target, List<EffectTrigger> value) =>
        target.SetField("triggers", value);

}
