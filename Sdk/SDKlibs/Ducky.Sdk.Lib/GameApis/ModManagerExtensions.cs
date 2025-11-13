using System;
using System.Runtime.CompilerServices;
using Duckov.Modding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Ducky.Sdk.GameApis;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class ModManagerExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform GetModParent(this ModManager target) =>
        target.GetField<ModManager, Transform>("modParent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ModManager SetModParent(this ModManager target, Transform value) =>
        target.SetField("modParent", value);

    // Static field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<ModInfo> GetStaticModInfos() =>
        FieldExtensions.GetStaticField<ModManager, List<ModInfo>>("modInfos");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticModInfos(List<ModInfo> value)
    {
        FieldExtensions.SetStaticField<ModManager, List<ModInfo>>("modInfos", value);
        return typeof(ModManager);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action<string, string> GetStaticOnModLoadingFailed() =>
        FieldExtensions.GetStaticField<ModManager, Action<string, string>>("OnModLoadingFailed");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticOnModLoadingFailed(Action<string, string> value)
    {
        FieldExtensions.SetStaticField<ModManager, Action<string, string>>("OnModLoadingFailed", value);
        return typeof(ModManager);
    }

}
