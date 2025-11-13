using System;
using System.Runtime.CompilerServices;
using Duckov.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class UIPrefabsReferenceExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UIPrefabsReference SetInventoryEntry(this UIPrefabsReference target, InventoryEntry value) =>
        target.SetField("inventoryEntry", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UIPrefabsReference SetItemDisplay(this UIPrefabsReference target, ItemDisplay value) =>
        target.SetField("itemDisplay", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UIPrefabsReference SetSlotDisplay(this UIPrefabsReference target, SlotDisplay value) =>
        target.SetField("slotDisplay", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UIPrefabsReference SetSlotIndicator(this UIPrefabsReference target, SlotIndicator value) =>
        target.SetField("slotIndicator", value);

}
