using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class LayersDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayerMask GetDamageReceiverLayerMask(this GameplayDataSettings.LayersData target) =>
        target.GetField<GameplayDataSettings.LayersData, LayerMask>("damageReceiverLayerMask");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.LayersData SetDamageReceiverLayerMask(
        this GameplayDataSettings.LayersData target, LayerMask value) =>
        target.SetField("damageReceiverLayerMask", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayerMask GetFowBlockLayers(this GameplayDataSettings.LayersData target) =>
        target.GetField<GameplayDataSettings.LayersData, LayerMask>("fowBlockLayers");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.LayersData SetFowBlockLayers(this GameplayDataSettings.LayersData target,
        LayerMask value) =>
        target.SetField("fowBlockLayers", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayerMask GetFowBlockLayersWithThermal(this GameplayDataSettings.LayersData target) =>
        target.GetField<GameplayDataSettings.LayersData, LayerMask>("fowBlockLayersWithThermal");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.LayersData SetFowBlockLayersWithThermal(
        this GameplayDataSettings.LayersData target, LayerMask value) =>
        target.SetField("fowBlockLayersWithThermal", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayerMask GetGroundLayerMask(this GameplayDataSettings.LayersData target) =>
        target.GetField<GameplayDataSettings.LayersData, LayerMask>("groundLayerMask");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.LayersData SetGroundLayerMask(this GameplayDataSettings.LayersData target,
        LayerMask value) =>
        target.SetField("groundLayerMask", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayerMask GetHalfObsticleLayer(this GameplayDataSettings.LayersData target) =>
        target.GetField<GameplayDataSettings.LayersData, LayerMask>("halfObsticleLayer");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.LayersData SetHalfObsticleLayer(this GameplayDataSettings.LayersData target,
        LayerMask value) =>
        target.SetField("halfObsticleLayer", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LayerMask GetWallLayerMask(this GameplayDataSettings.LayersData target) =>
        target.GetField<GameplayDataSettings.LayersData, LayerMask>("wallLayerMask");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.LayersData SetWallLayerMask(this GameplayDataSettings.LayersData target,
        LayerMask value) =>
        target.SetField("wallLayerMask", value);
}
