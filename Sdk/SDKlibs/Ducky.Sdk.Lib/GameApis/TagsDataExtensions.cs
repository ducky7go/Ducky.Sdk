using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class TagsDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetAdvancedDebuffMode(this GameplayDataSettings.TagsData target,
        Tag value) =>
        target.SetField("advancedDebuffMode", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData
        SetAllTags(this GameplayDataSettings.TagsData target, List<Tag> value) =>
        target.SetField("allTags", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetArmor(this GameplayDataSettings.TagsData target, Tag value) =>
        target.SetField("armor", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetBackpack(this GameplayDataSettings.TagsData target, Tag value) =>
        target.SetField("backpack", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetBait(this GameplayDataSettings.TagsData target, Tag value) =>
        target.SetField("bait", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetBullet(this GameplayDataSettings.TagsData target, Tag value) =>
        target.SetField("bullet", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetCharacter(this GameplayDataSettings.TagsData target, Tag value) =>
        target.SetField("character", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetDestroyOnLootBox(this GameplayDataSettings.TagsData target,
        Tag value) =>
        target.SetField("destroyOnLootBox", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetDontDropOnDeadInSlot(this GameplayDataSettings.TagsData target,
        Tag value) =>
        target.SetField("dontDropOnDeadInSlot", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetHelmat(this GameplayDataSettings.TagsData target, Tag value) =>
        target.SetField("helmat", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData
        SetLockInDemoTag(this GameplayDataSettings.TagsData target, Tag value) =>
        target.SetField("lockInDemoTag", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.TagsData SetSpecial(this GameplayDataSettings.TagsData target, Tag value) =>
        target.SetField("special", value);
}
