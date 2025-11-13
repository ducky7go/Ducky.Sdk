using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class PrefabsDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetAlertFxPrefab(this GameplayDataSettings.PrefabsData target, GameObject value) =>
        target.SetField("alertFxPrefab", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetBuildingBlockAreaMesh(this GameplayDataSettings.PrefabsData target, GameObject value) =>
        target.SetField("buildingBlockAreaMesh", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetBulletHitObsticleFx(this GameplayDataSettings.PrefabsData target, GameObject value) =>
        target.SetField("bulletHitObsticleFx", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetCharacterPrefab(this GameplayDataSettings.PrefabsData target, CharacterMainControl value) =>
        target.SetField("characterPrefab", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetDefaultBullet(this GameplayDataSettings.PrefabsData target, Projectile value) =>
        target.SetField("defaultBullet", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetHandheldAgentPrefab(this GameplayDataSettings.PrefabsData target, DuckovItemAgent value) =>
        target.SetField("handheldAgentPrefab", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetHeadCollider(this GameplayDataSettings.PrefabsData target, HeadCollider value) =>
        target.SetField("headCollider", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetInteractMarker(this GameplayDataSettings.PrefabsData target, InteractMarker value) =>
        target.SetField("interactMarker", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetKazooUi(this GameplayDataSettings.PrefabsData target, GameObject value) =>
        target.SetField("kazooUi", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetLevelManagerPrefab(this GameplayDataSettings.PrefabsData target, LevelManager value) =>
        target.SetField("levelManagerPrefab", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetLootBoxPrefab(this GameplayDataSettings.PrefabsData target, InteractableLootbox value) =>
        target.SetField("lootBoxPrefab", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InteractableLootbox GetLootBoxPrefabTomb(this GameplayDataSettings.PrefabsData target) =>
        target.GetField<GameplayDataSettings.PrefabsData, InteractableLootbox>("lootBoxPrefab_Tomb");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetLootBoxPrefabTomb(this GameplayDataSettings.PrefabsData target, InteractableLootbox value) =>
        target.SetField("lootBoxPrefab_Tomb", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetPickupAgentNoRendererPrefab(this GameplayDataSettings.PrefabsData target, DuckovItemAgent value) =>
        target.SetField("pickupAgentNoRendererPrefab", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetPickupAgentPrefab(this GameplayDataSettings.PrefabsData target, DuckovItemAgent value) =>
        target.SetField("pickupAgentPrefab", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetQuestMarker(this GameplayDataSettings.PrefabsData target, GameObject value) =>
        target.SetField("questMarker", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UIInputManager GetUiInputManagerPrefab(this GameplayDataSettings.PrefabsData target) =>
        target.GetField<GameplayDataSettings.PrefabsData, UIInputManager>("uiInputManagerPrefab");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.PrefabsData SetUiInputManagerPrefab(this GameplayDataSettings.PrefabsData target, UIInputManager value) =>
        target.SetField("uiInputManagerPrefab", value);

}
