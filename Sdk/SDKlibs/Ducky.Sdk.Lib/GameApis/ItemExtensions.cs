using System;
using System.Runtime.CompilerServices;
using ItemStatsSystem;
using Duckov.Utilities;
using ItemStatsSystem.Items;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class ItemExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetAgentUtilities(this Item target, ItemAgentUtilities value) =>
        target.SetField("agentUtilities", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetConstants(this Item target, CustomDataCollection value) =>
        target.SetField("constants", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetDisplayName(this Item target, string value) =>
        target.SetField("displayName", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetDisplayQuality(this Item target, DisplayQuality value) =>
        target.SetField("displayQuality", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetEffects(this Item target, List<Effect> value) =>
        target.SetField("effects", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetIcon(this Item target, Sprite value) =>
        target.SetField("icon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetInventory(this Item target, Inventory value) =>
        target.SetField("inventory", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetItemGraphic(this Item target, ItemGraphicInfo value) =>
        target.SetField("itemGraphic", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetMaxStackCount(this Item target, int value) =>
        target.SetField("maxStackCount", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetModifiers(this Item target, ModifierDescriptionCollection value) =>
        target.SetField("modifiers", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetOrder(this Item target, int value) =>
        target.SetField("order", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetQuality(this Item target, int value) =>
        target.SetField("quality", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetSlots(this Item target, SlotCollection value) =>
        target.SetField("slots", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetSoundKey(this Item target, string value) =>
        target.SetField("soundKey", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetStats(this Item target, StatCollection value) =>
        target.SetField("stats", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetTags(this Item target, TagCollection value) =>
        target.SetField("tags", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetTypeID(this Item target, int value) =>
        target.SetField("typeID", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetUsageUtilities(this Item target, UsageUtilities value) =>
        target.SetField("usageUtilities", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetValue(this Item target, int value) =>
        target.SetField("value", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetVariables(this Item target, CustomDataCollection value) =>
        target.SetField("variables", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetWeight(this Item target) =>
        target.GetField<Item, float>("weight");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item SetWeight(this Item target, float value) =>
        target.SetField("weight", value);

}
