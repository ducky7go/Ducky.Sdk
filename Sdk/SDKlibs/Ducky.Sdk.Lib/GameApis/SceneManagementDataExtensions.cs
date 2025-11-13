using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using Eflatun.SceneReference;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class SceneManagementDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.SceneManagementData SetBaseScene(
        this GameplayDataSettings.SceneManagementData target, SceneReference value) =>
        target.SetField("baseScene", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.SceneManagementData SetEvacuateScreenScene(
        this GameplayDataSettings.SceneManagementData target, SceneReference value) =>
        target.SetField("evacuateScreenScene", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.SceneManagementData SetFailLoadingScreenScene(
        this GameplayDataSettings.SceneManagementData target, SceneReference value) =>
        target.SetField("failLoadingScreenScene", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.SceneManagementData SetMainMenuScene(
        this GameplayDataSettings.SceneManagementData target, SceneReference value) =>
        target.SetField("mainMenuScene", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.SceneManagementData SetPrologueScene(
        this GameplayDataSettings.SceneManagementData target, SceneReference value) =>
        target.SetField("prologueScene", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.SceneManagementData SetSceneInfoCollection(
        this GameplayDataSettings.SceneManagementData target, SceneInfoCollection value) =>
        target.SetField("sceneInfoCollection", value);
}
