using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class LootingDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float[] GetInspectingTimes(this GameplayDataSettings.LootingData target) =>
        target.GetField<GameplayDataSettings.LootingData, float[]>("inspectingTimes");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.LootingData SetInspectingTimes(this GameplayDataSettings.LootingData target,
        float[] value) =>
        target.SetField("inspectingTimes", value);
}
