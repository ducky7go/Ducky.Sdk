using System;
using System.Runtime.CompilerServices;
using Duckov;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Ducky.Sdk.GameApis;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class EXPManagerExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLevelChangeNotificationFormatKey(this EXPManager target) =>
        target.GetField<EXPManager, string>("levelChangeNotificationFormatKey");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EXPManager SetLevelChangeNotificationFormatKey(this EXPManager target, string value) =>
        target.SetField("levelChangeNotificationFormatKey", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<Int64> GetLevelExpDefinition(this EXPManager target) =>
        target.GetField<EXPManager, List<Int64>>("levelExpDefinition");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EXPManager SetLevelExpDefinition(this EXPManager target, List<Int64> value) =>
        target.SetField("levelExpDefinition", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int64 GetPoint(this EXPManager target) =>
        target.GetField<EXPManager, Int64>("point");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EXPManager SetPoint(this EXPManager target, Int64 value) =>
        target.SetField("point", value);

    // Static field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action<Int64> GetStaticOnExpChanged() =>
        FieldExtensions.GetStaticField<EXPManager, Action<Int64>>("onExpChanged");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticOnExpChanged(Action<Int64> value)
    {
        FieldExtensions.SetStaticField<EXPManager, Action<Int64>>("onExpChanged", value);
        return typeof(EXPManager);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action<int, int> GetStaticOnLevelChanged() =>
        FieldExtensions.GetStaticField<EXPManager, Action<int, int>>("onLevelChanged");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticOnLevelChanged(Action<int, int> value)
    {
        FieldExtensions.SetStaticField<EXPManager, Action<int, int>>("onLevelChanged", value);
        return typeof(EXPManager);
    }
}
