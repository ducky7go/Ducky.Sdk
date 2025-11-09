using System.Collections.Generic;
using Duckov.Buffs;
using Duckov.Utilities;

namespace Ducky.Sdk.GameApis;

public static class BuffsDataExtensions
{
    // Instance field accessors
    public static List<Buff> GetAllBuffs(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, List<Buff>>("allBuffs");

    public static void SetAllBuffs(this GameplayDataSettings.BuffsData target, List<Buff> value) =>
        target.SetField("allBuffs", value);

    public static void SetBaseBuff(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("baseBuff", value);

    public static void SetBleedSBuff(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("bleedSBuff", value);

    public static void SetBoneCrackBuff(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("boneCrackBuff", value);

    public static void SetBurn(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("burn", value);

    public static void SetElectric(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("electric", value);

    public static void SetPain(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("pain", value);

    public static void SetPoison(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("poison", value);

    public static void SetSpace(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("space", value);

    public static void SetStarve(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("starve", value);

    public static void SetThirsty(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("thirsty", value);

    public static void SetUnlimitBleedBuff(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("unlimitBleedBuff", value);

    public static Buff GetWeightHeavy(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, Buff>("weight_Heavy");

    public static void SetWeightHeavy(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("weight_Heavy", value);

    public static Buff GetWeightLight(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, Buff>("weight_Light");

    public static void SetWeightLight(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("weight_Light", value);

    public static Buff GetWeightOverweight(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, Buff>("weight_Overweight");

    public static void SetWeightOverweight(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("weight_Overweight", value);

    public static Buff GetWeightSuperHeavy(this GameplayDataSettings.BuffsData target) =>
        target.GetField<GameplayDataSettings.BuffsData, Buff>("weight_SuperHeavy");

    public static void SetWeightSuperHeavy(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("weight_SuperHeavy", value);

    public static void SetWoundBuff(this GameplayDataSettings.BuffsData target, Buff value) =>
        target.SetField("woundBuff", value);

}
