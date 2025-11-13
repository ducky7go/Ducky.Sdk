using System;
using System.Runtime.CompilerServices;
using Duckov.Buildings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class BuildingDataCollectionExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BuildingDataCollection SetInfos(this BuildingDataCollection target, List<BuildingInfo> value) =>
        target.SetField("infos", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<Building> GetPrefabs(this BuildingDataCollection target) =>
        target.GetField<BuildingDataCollection, List<Building>>("prefabs");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BuildingDataCollection SetPrefabs(this BuildingDataCollection target, List<Building> value) =>
        target.SetField("prefabs", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyCollection<BuildingInfo> GetReadonlyInfos(this BuildingDataCollection target) =>
        target.GetField<BuildingDataCollection, ReadOnlyCollection<BuildingInfo>>("readonlyInfos");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BuildingDataCollection SetReadonlyInfos(this BuildingDataCollection target,
        ReadOnlyCollection<BuildingInfo> value) =>
        target.SetField("readonlyInfos", value);
}
