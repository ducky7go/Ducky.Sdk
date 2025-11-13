using System;
using System.Runtime.CompilerServices;
using Duckov.Rules;
using Duckov.Scenes;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Ducky.Sdk.GameApis;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class LevelManagerExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AIMainBrain GetAiMainBrain(this LevelManager target) =>
        target.GetField<LevelManager, AIMainBrain>("aiMainBrain");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetAiMainBrain(this LevelManager target, AIMainBrain value) =>
        target.SetField("aiMainBrain", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetBulletPool(this LevelManager target, BulletPool value) =>
        target.SetField("bulletPool", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetCharacterCreator(this LevelManager target, CharacterCreator value) =>
        target.SetField("characterCreator", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sprite GetCharacterMapIcon(this LevelManager target) =>
        target.GetField<LevelManager, Sprite>("characterMapIcon");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetCharacterMapIcon(this LevelManager target, Sprite value) =>
        target.SetField("characterMapIcon", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color GetCharacterMapIconColor(this LevelManager target) =>
        target.GetField<LevelManager, Color>("characterMapIconColor");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetCharacterMapIconColor(this LevelManager target, Color value) =>
        target.SetField("characterMapIconColor", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color GetCharacterMapShadowColor(this LevelManager target) =>
        target.GetField<LevelManager, Color>("characterMapShadowColor");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetCharacterMapShadowColor(this LevelManager target, Color value) =>
        target.SetField("characterMapShadowColor", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterModel GetCharacterModel(this LevelManager target) =>
        target.GetField<LevelManager, CharacterModel>("characterModel");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetCharacterModel(this LevelManager target, CharacterModel value) =>
        target.SetField("characterModel", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetCustomFaceManager(this LevelManager target, CustomFaceManager value) =>
        target.SetField("customFaceManager", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SkillBase GetDefaultSkill(this LevelManager target) =>
        target.GetField<LevelManager, SkillBase>("defaultSkill");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetDefaultSkill(this LevelManager target, SkillBase value) =>
        target.SetField("defaultSkill", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform GetDefaultStartPos(this LevelManager target) =>
        target.GetField<LevelManager, Transform>("defaultStartPos");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetDefaultStartPos(this LevelManager target, Transform value) =>
        target.SetField("defaultStartPos", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetExitCreator(this LevelManager target, ExitCreator value) =>
        target.SetField("exitCreator", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetExplosionManager(this LevelManager target, ExplosionManager value) =>
        target.SetField("explosionManager", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FogOfWarManager GetFowManager(this LevelManager target) =>
        target.GetField<LevelManager, FogOfWarManager>("fowManager");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetFowManager(this LevelManager target, FogOfWarManager value) =>
        target.SetField("fowManager", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetGameCamera(this LevelManager target, GameCamera value) =>
        target.SetField("gameCamera", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetInputManager(this LevelManager target, InputManager value) =>
        target.SetField("inputManager", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterRandomPreset GetMatePreset(this LevelManager target) =>
        target.GetField<LevelManager, CharacterRandomPreset>("matePreset");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetMatePreset(this LevelManager target, CharacterRandomPreset value) =>
        target.SetField("matePreset", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterRandomPreset GetPetPreset(this LevelManager target) =>
        target.GetField<LevelManager, CharacterRandomPreset>("petPreset");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetPetPreset(this LevelManager target, CharacterRandomPreset value) =>
        target.SetField("petPreset", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetPetProxy(this LevelManager target, PetProxy value) =>
        target.SetField("petProxy", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MultiSceneLocation GetTestTeleportTarget(this LevelManager target) =>
        target.GetField<LevelManager, MultiSceneLocation>("testTeleportTarget");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetTestTeleportTarget(this LevelManager target, MultiSceneLocation value) =>
        target.SetField("testTeleportTarget", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LevelManager SetTimeOfDayController(this LevelManager target, TimeOfDayController value) =>
        target.SetField("timeOfDayController", value);

    // Static field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetStaticEnemySpawnCountFactor() =>
        FieldExtensions.GetStaticField<LevelManager, float>("enemySpawnCountFactor");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticEnemySpawnCountFactor(float value)
    {
        FieldExtensions.SetStaticField<LevelManager, float>("enemySpawnCountFactor", value);
        return typeof(LevelManager);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetStaticForceBossSpawn() =>
        FieldExtensions.GetStaticField<LevelManager, bool>("forceBossSpawn");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticForceBossSpawn(bool value)
    {
        FieldExtensions.SetStaticField<LevelManager, bool>("forceBossSpawn", value);
        return typeof(LevelManager);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStaticLoadLevelBeaconIndex() =>
        FieldExtensions.GetStaticField<LevelManager, int>("loadLevelBeaconIndex");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticLoadLevelBeaconIndex(int value)
    {
        FieldExtensions.SetStaticField<LevelManager, int>("loadLevelBeaconIndex", value);
        return typeof(LevelManager);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetStaticMainCharacterHealthSaveKey() =>
        FieldExtensions.GetStaticField<LevelManager, string>("MainCharacterHealthSaveKey");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticMainCharacterHealthSaveKey(string value)
    {
        FieldExtensions.SetStaticField<LevelManager, string>("MainCharacterHealthSaveKey", value);
        return typeof(LevelManager);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetStaticMainCharacterItemSaveKey() =>
        FieldExtensions.GetStaticField<LevelManager, string>("MainCharacterItemSaveKey");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticMainCharacterItemSaveKey(string value)
    {
        FieldExtensions.SetStaticField<LevelManager, string>("MainCharacterItemSaveKey", value);
        return typeof(LevelManager);
    }
}
