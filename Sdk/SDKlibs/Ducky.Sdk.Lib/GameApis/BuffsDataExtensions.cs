using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using Duckov.Buffs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class BuffsDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<Buff> GetAllBuffs(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, List<Buff>>("allBuffs");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetAllBuffs(this GameplayDataSettings.BuffsData target,
        List<Buff> value) =>
        target.SetField("allBuffs", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetBaseBuff(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("baseBuff", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData
        SetBleedSBuff(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("bleedSBuff", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetBoneCrackBuff(this GameplayDataSettings.BuffsData target,
        Buff value) =>
        target.SetField("boneCrackBuff", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetBurn(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("burn", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetElectric(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("electric", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetPain(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("pain", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetPoison(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("poison", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetSpace(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("space", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetStarve(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("starve", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetThirsty(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("thirsty", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetUnlimitBleedBuff(this GameplayDataSettings.BuffsData target,
        Buff value) =>
        target.SetField("unlimitBleedBuff", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff GetWeightHeavy(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, Buff>("weight_Heavy");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData
        SetWeightHeavy(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("weight_Heavy", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff GetWeightLight(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, Buff>("weight_Light");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData
        SetWeightLight(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("weight_Light", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff GetWeightOverweight(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, Buff>("weight_Overweight");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetWeightOverweight(this GameplayDataSettings.BuffsData target,
        Buff value) =>
        target.SetField("weight_Overweight", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Buff GetWeightSuperHeavy(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, Buff>("weight_SuperHeavy");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetWeightSuperHeavy(this GameplayDataSettings.BuffsData target,
        Buff value) =>
        target.SetField("weight_SuperHeavy", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.BuffsData SetWoundBuff(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("woundBuff", value);
}
