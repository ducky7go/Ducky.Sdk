using System;
using System.Runtime.CompilerServices;
using Duckov.Crops;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class CropDatabaseExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<CropInfo> GetEntries(this CropDatabase target) =>
        target.GetField<CropDatabase, List<CropInfo>>("entries");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CropDatabase SetEntries(this CropDatabase target, List<CropInfo> value) =>
        target.SetField("entries", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<SeedInfo> GetSeedInfos(this CropDatabase target) =>
        target.GetField<CropDatabase, List<SeedInfo>>("seedInfos");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CropDatabase SetSeedInfos(this CropDatabase target, List<SeedInfo> value) =>
        target.SetField("seedInfos", value);
}
