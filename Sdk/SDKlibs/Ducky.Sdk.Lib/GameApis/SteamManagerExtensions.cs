using System;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Ducky.Sdk.GameApis;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class SteamManagerExtensions
{
    // Static field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStaticAppIDInt() =>
        FieldExtensions.GetStaticField<SteamManager, int>("AppID_Int");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticAppIDInt(int value)
    {
        FieldExtensions.SetStaticField<SteamManager, int>("AppID_Int", value);
        return typeof(SteamManager);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetStaticSteamEnabled() =>
        FieldExtensions.GetStaticField<SteamManager, bool>("SteamEnabled");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticSteamEnabled(bool value)
    {
        FieldExtensions.SetStaticField<SteamManager, bool>("SteamEnabled", value);
        return typeof(SteamManager);
    }
}
