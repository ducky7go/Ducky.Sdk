using System;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class CustomFaceDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetDecorations(this CustomFaceData target, CustomFacePartCollection value) =>
        target.SetField("decorations", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetDefaultPreset(this CustomFaceData target, CustomFacePreset value) =>
        target.SetField("defaultPreset", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetEyebrows(this CustomFaceData target, CustomFacePartCollection value) =>
        target.SetField("eyebrows", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetEyes(this CustomFaceData target, CustomFacePartCollection value) =>
        target.SetField("eyes", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetFoots(this CustomFaceData target, CustomFacePartCollection value) =>
        target.SetField("foots", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetHairs(this CustomFaceData target, CustomFacePartCollection value) =>
        target.SetField("hairs", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetInfo(this CustomFaceData target) =>
        target.GetField<CustomFaceData, string>("info");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetInfo(this CustomFaceData target, string value) =>
        target.SetField("info", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetMouths(this CustomFaceData target, CustomFacePartCollection value) =>
        target.SetField("mouths", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetPrefabsPath(this CustomFaceData target) =>
        target.GetField<CustomFaceData, string>("prefabsPath");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetPrefabsPath(this CustomFaceData target, string value) =>
        target.SetField("prefabsPath", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetTails(this CustomFaceData target, CustomFacePartCollection value) =>
        target.SetField("tails", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CustomFaceData SetWings(this CustomFaceData target, CustomFacePartCollection value) =>
        target.SetField("wings", value);
}
