using System;
using System.Runtime.CompilerServices;
using Duckov;
using Duckov.Buffs;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Ducky.Sdk.GameApis;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class CharacterMainControlExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgentHolder GetAgentHolder(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, ItemAgentHolder>("agentHolder");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetAgentHolder(this CharacterMainControl target, ItemAgentHolder value) =>
        target.SetField("agentHolder", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CA_Attack GetAttackAction(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CA_Attack>("attackAction");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetAttackAction(this CharacterMainControl target, CA_Attack value) =>
        target.SetField("attackAction", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterBuffManager GetBuffManager(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CharacterBuffManager>("buffManager");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetBuffManager(this CharacterMainControl target, CharacterBuffManager value) =>
        target.SetField("buffManager", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<Buff.BuffExclusiveTags> GetBuffResist(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, List<Buff.BuffExclusiveTags>>("buffResist");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetBuffResist(this CharacterMainControl target, List<Buff.BuffExclusiveTags> value) =>
        target.SetField("buffResist", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CA_Carry GetCarryAction(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CA_Carry>("carryAction");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetCarryAction(this CharacterMainControl target, CA_Carry value) =>
        target.SetField("carryAction", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterModel GetCharacterModel(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CharacterModel>("characterModel");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetCharacterModel(this CharacterMainControl target, CharacterModel value) =>
        target.SetField("characterModel", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterRandomPreset GetCharacterPreset(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CharacterRandomPreset>("characterPreset");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetCharacterPreset(this CharacterMainControl target, CharacterRandomPreset value) =>
        target.SetField("characterPreset", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CA_Dash GetDashAction(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CA_Dash>("dashAction");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetDashAction(this CharacterMainControl target, CA_Dash value) =>
        target.SetField("dashAction", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InteractableLootbox GetDeadLootBoxPrefab(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, InteractableLootbox>("deadLootBoxPrefab");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetDeadLootBoxPrefab(this CharacterMainControl target, InteractableLootbox value) =>
        target.SetField("deadLootBoxPrefab", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetEquipmentController(this CharacterMainControl target, CharacterEquipmentController value) =>
        target.SetField("equipmentController", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetHealth(this CharacterMainControl target, Health value) =>
        target.SetField("health", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CA_Interact GetInteractAction(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CA_Interact>("interactAction");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetInteractAction(this CharacterMainControl target, CA_Interact value) =>
        target.SetField("interactAction", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterItemControl GetItemControl(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CharacterItemControl>("itemControl");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetItemControl(this CharacterMainControl target, CharacterItemControl value) =>
        target.SetField("itemControl", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DamageReceiver GetMainDamageReceiver(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, DamageReceiver>("mainDamageReceiver");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetMainDamageReceiver(this CharacterMainControl target, DamageReceiver value) =>
        target.SetField("mainDamageReceiver", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform GetModelRoot(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, Transform>("modelRoot");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetModelRoot(this CharacterMainControl target, Transform value) =>
        target.SetField("modelRoot", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Movement GetMovementControl(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, Movement>("movementControl");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetMovementControl(this CharacterMainControl target, Movement value) =>
        target.SetField("movementControl", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CA_Reload GetReloadAction(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CA_Reload>("reloadAction");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetReloadAction(this CharacterMainControl target, CA_Reload value) =>
        target.SetField("reloadAction", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CA_Skill GetSkillAction(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CA_Skill>("skillAction");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetSkillAction(this CharacterMainControl target, CA_Skill value) =>
        target.SetField("skillAction", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetTeam(this CharacterMainControl target, Teams value) =>
        target.SetField("team", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CA_UseItem GetUseItemAction(this CharacterMainControl target) =>
        target.GetField<CharacterMainControl, CA_UseItem>("useItemAction");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl SetUseItemAction(this CharacterMainControl target, CA_UseItem value) =>
        target.SetField("useItemAction", value);

    // Static field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action<CharacterMainControl, DuckovItemAgent> GetStaticOnMainCharacterChangeHoldItemAgentEvent() =>
        FieldExtensions.GetStaticField<CharacterMainControl, Action<CharacterMainControl, DuckovItemAgent>>("OnMainCharacterChangeHoldItemAgentEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticOnMainCharacterChangeHoldItemAgentEvent(Action<CharacterMainControl, DuckovItemAgent> value)
    {
        FieldExtensions.SetStaticField<CharacterMainControl, Action<CharacterMainControl, DuckovItemAgent>>("OnMainCharacterChangeHoldItemAgentEvent", value);
        return typeof(CharacterMainControl);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action<CharacterMainControl, Inventory, int> GetStaticOnMainCharacterInventoryChangedEvent() =>
        FieldExtensions.GetStaticField<CharacterMainControl, Action<CharacterMainControl, Inventory, int>>("OnMainCharacterInventoryChangedEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticOnMainCharacterInventoryChangedEvent(Action<CharacterMainControl, Inventory, int> value)
    {
        FieldExtensions.SetStaticField<CharacterMainControl, Action<CharacterMainControl, Inventory, int>>("OnMainCharacterInventoryChangedEvent", value);
        return typeof(CharacterMainControl);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action<CharacterMainControl, Slot> GetStaticOnMainCharacterSlotContentChangedEvent() =>
        FieldExtensions.GetStaticField<CharacterMainControl, Action<CharacterMainControl, Slot>>("OnMainCharacterSlotContentChangedEvent");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticOnMainCharacterSlotContentChangedEvent(Action<CharacterMainControl, Slot> value)
    {
        FieldExtensions.SetStaticField<CharacterMainControl, Action<CharacterMainControl, Slot>>("OnMainCharacterSlotContentChangedEvent", value);
        return typeof(CharacterMainControl);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStaticWeightThresholdHeavy() =>
        FieldExtensions.GetStaticField<CharacterMainControl, float>("weightThreshold_Heavy");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticWeightThresholdHeavy(float value)
    {
        FieldExtensions.SetStaticField<CharacterMainControl, float>("weightThreshold_Heavy", value);
        return typeof(CharacterMainControl);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStaticWeightThresholdLight() =>
        FieldExtensions.GetStaticField<CharacterMainControl, float>("weightThreshold_Light");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticWeightThresholdLight(float value)
    {
        FieldExtensions.SetStaticField<CharacterMainControl, float>("weightThreshold_Light", value);
        return typeof(CharacterMainControl);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStaticWeightThresholdSuperWeight() =>
        FieldExtensions.GetStaticField<CharacterMainControl, float>("weightThreshold_superWeight");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticWeightThresholdSuperWeight(float value)
    {
        FieldExtensions.SetStaticField<CharacterMainControl, float>("weightThreshold_superWeight", value);
        return typeof(CharacterMainControl);
    }

}
