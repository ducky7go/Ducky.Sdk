using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class ItemAssetsDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.ItemAssetsData SetCashItemTypeID(this GameplayDataSettings.ItemAssetsData target,
        int value) =>
        target.SetField("cashItemTypeID", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.ItemAssetsData SetDefaultCharacterItemTypeID(
        this GameplayDataSettings.ItemAssetsData target, int value) =>
        target.SetField("defaultCharacterItemTypeID", value);
}
