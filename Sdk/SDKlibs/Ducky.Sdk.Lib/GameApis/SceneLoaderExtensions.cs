using System;
using System.Runtime.CompilerServices;
using Duckov.UI.Animations;
using Eflatun.SceneReference;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class SceneLoaderExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FadeGroup GetClickIndicator(this SceneLoader target) =>
        target.GetField<SceneLoader, FadeGroup>("clickIndicator");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetClickIndicator(this SceneLoader target, FadeGroup value) =>
        target.SetField("clickIndicator", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FadeGroup GetContent(this SceneLoader target) =>
        target.GetField<SceneLoader, FadeGroup>("content");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetContent(this SceneLoader target, FadeGroup value) =>
        target.SetField("content", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneReference GetDefaultCurtainScene(this SceneLoader target) =>
        target.GetField<SceneLoader, SceneReference>("defaultCurtainScene");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetDefaultCurtainScene(this SceneLoader target, SceneReference value) =>
        target.SetField("defaultCurtainScene", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AnimationCurve GetFadeCurve1(this SceneLoader target) =>
        target.GetField<SceneLoader, AnimationCurve>("fadeCurve1");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetFadeCurve1(this SceneLoader target, AnimationCurve value) =>
        target.SetField("fadeCurve1", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AnimationCurve GetFadeCurve2(this SceneLoader target) =>
        target.GetField<SceneLoader, AnimationCurve>("fadeCurve2");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetFadeCurve2(this SceneLoader target, AnimationCurve value) =>
        target.SetField("fadeCurve2", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AnimationCurve GetFadeCurve3(this SceneLoader target) =>
        target.GetField<SceneLoader, AnimationCurve>("fadeCurve3");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetFadeCurve3(this SceneLoader target, AnimationCurve value) =>
        target.SetField("fadeCurve3", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AnimationCurve GetFadeCurve4(this SceneLoader target) =>
        target.GetField<SceneLoader, AnimationCurve>("fadeCurve4");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetFadeCurve4(this SceneLoader target, AnimationCurve value) =>
        target.SetField("fadeCurve4", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FadeGroup GetLoadingIndicator(this SceneLoader target) =>
        target.GetField<SceneLoader, FadeGroup>("loadingIndicator");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetLoadingIndicator(this SceneLoader target, FadeGroup value) =>
        target.SetField("loadingIndicator", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetMinimumLoadingTime(this SceneLoader target) =>
        target.GetField<SceneLoader, float>("minimumLoadingTime");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetMinimumLoadingTime(this SceneLoader target, float value) =>
        target.SetField("minimumLoadingTime", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OnPointerClick GetPointerClickEventRecevier(this SceneLoader target) =>
        target.GetField<SceneLoader, OnPointerClick>("pointerClickEventRecevier");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetPointerClickEventRecevier(this SceneLoader target, OnPointerClick value) =>
        target.SetField("pointerClickEventRecevier", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneReference GetTarget(this SceneLoader target) =>
        target.GetField<SceneLoader, SceneReference>("target");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetTarget(this SceneLoader target, SceneReference value) =>
        target.SetField("target", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetWaitAfterSceneLoaded(this SceneLoader target) =>
        target.GetField<SceneLoader, float>("waitAfterSceneLoaded");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SceneLoader SetWaitAfterSceneLoaded(this SceneLoader target, float value) =>
        target.SetField("waitAfterSceneLoaded", value);

}
