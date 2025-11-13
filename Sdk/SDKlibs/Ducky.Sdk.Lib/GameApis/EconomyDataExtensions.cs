using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class EconomyDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<int> GetUnlockItemByDefault(this GameplayDataSettings.EconomyData target) =>
        target.GetField<GameplayDataSettings.EconomyData, List<int>>("unlockItemByDefault");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.EconomyData SetUnlockItemByDefault(this GameplayDataSettings.EconomyData target,
        List<int> value) =>
        target.SetField("unlockItemByDefault", value);
}
