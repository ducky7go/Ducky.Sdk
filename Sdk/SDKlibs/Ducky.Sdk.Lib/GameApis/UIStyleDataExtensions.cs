using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class UIStyleDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetBossCharacterIcon(this GameplayDataSettings.UIStyleData target, Sprite value) =>
        target.SetField("bossCharacterIcon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetCritPopSprite(this GameplayDataSettings.UIStyleData target, Sprite value) =>
        target.SetField("critPopSprite", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color GetDefaultDisplayQualityShadowColor(this GameplayDataSettings.UIStyleData target) =>
        target.GetField<GameplayDataSettings.UIStyleData, Color>("defaultDisplayQualityShadowColor");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetDefaultDisplayQualityShadowColor(this GameplayDataSettings.UIStyleData target, Color value) =>
        target.SetField("defaultDisplayQualityShadowColor", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetDefaultDIsplayQualityShadowInnerGlow(this GameplayDataSettings.UIStyleData target) =>
        target.GetField<GameplayDataSettings.UIStyleData, bool>("defaultDIsplayQualityShadowInnerGlow");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetDefaultDIsplayQualityShadowInnerGlow(this GameplayDataSettings.UIStyleData target, bool value) =>
        target.SetField("defaultDIsplayQualityShadowInnerGlow", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetDefaultDisplayQualityShadowOffset(this GameplayDataSettings.UIStyleData target) =>
        target.GetField<GameplayDataSettings.UIStyleData, float>("defaultDisplayQualityShadowOffset");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetDefaultDisplayQualityShadowOffset(this GameplayDataSettings.UIStyleData target, float value) =>
        target.SetField("defaultDisplayQualityShadowOffset", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetDefaultFont(this GameplayDataSettings.UIStyleData target, TMP_Asset value) =>
        target.SetField("defaultFont", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetDefaultTeleporterIcon(this GameplayDataSettings.UIStyleData target, Sprite value) =>
        target.SetField("defaultTeleporterIcon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<GameplayDataSettings.UIStyleData.DisplayQualityLook> GetDisplayQualityLooks(this GameplayDataSettings.UIStyleData target) =>
        target.GetField<GameplayDataSettings.UIStyleData, List<GameplayDataSettings.UIStyleData.DisplayQualityLook>>("displayQualityLooks");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetDisplayQualityLooks(this GameplayDataSettings.UIStyleData target, List<GameplayDataSettings.UIStyleData.DisplayQualityLook> value) =>
        target.SetField("displayQualityLooks", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<GameplayDataSettings.UIStyleData.DisplayElementDamagePopTextLook> GetElementDamagePopTextLook(this GameplayDataSettings.UIStyleData target) =>
        target.GetField<GameplayDataSettings.UIStyleData, List<GameplayDataSettings.UIStyleData.DisplayElementDamagePopTextLook>>("elementDamagePopTextLook");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetElementDamagePopTextLook(this GameplayDataSettings.UIStyleData target, List<GameplayDataSettings.UIStyleData.DisplayElementDamagePopTextLook> value) =>
        target.SetField("elementDamagePopTextLook", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetEleteCharacterIcon(this GameplayDataSettings.UIStyleData target, Sprite value) =>
        target.SetField("eleteCharacterIcon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetFallbackItemIcon(this GameplayDataSettings.UIStyleData target, Sprite value) =>
        target.SetField("fallbackItemIcon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetMerchantCharacterIcon(this GameplayDataSettings.UIStyleData target, Sprite value) =>
        target.SetField("merchantCharacterIcon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetPetCharacterIcon(this GameplayDataSettings.UIStyleData target, Sprite value) =>
        target.SetField("petCharacterIcon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetPmcCharacterIcon(this GameplayDataSettings.UIStyleData target, Sprite value) =>
        target.SetField("pmcCharacterIcon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetTeleporterIconScale(this GameplayDataSettings.UIStyleData target, float value) =>
        target.SetField("teleporterIconScale", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.UIStyleData SetTemplateTextUGUI(this GameplayDataSettings.UIStyleData target, TextMeshProUGUI value) =>
        target.SetField("templateTextUGUI", value);

}
