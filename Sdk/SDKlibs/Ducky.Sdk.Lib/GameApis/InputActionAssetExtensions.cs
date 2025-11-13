using System;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.Utilities;
using Ducky.Sdk.GameApis;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class InputActionAssetExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputActionMap[] GetMActionMaps(this InputActionAsset target) =>
        target.GetField<InputActionAsset, InputActionMap[]>("m_ActionMaps");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputActionAsset SetMActionMaps(this InputActionAsset target, InputActionMap[] value) =>
        target.SetField("m_ActionMaps", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputControlScheme[] GetMControlSchemes(this InputActionAsset target) =>
        target.GetField<InputActionAsset, InputControlScheme[]>("m_ControlSchemes");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputActionAsset SetMControlSchemes(this InputActionAsset target, InputControlScheme[] value) =>
        target.SetField("m_ControlSchemes", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetMIsProjectWide(this InputActionAsset target) =>
        target.GetField<InputActionAsset, bool>("m_IsProjectWide");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static InputActionAsset SetMIsProjectWide(this InputActionAsset target, bool value) =>
        target.SetField("m_IsProjectWide", value);

    // Static field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetStaticExtension() =>
        FieldExtensions.GetStaticField<InputActionAsset, string>("Extension");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticExtension(string value)
    {
        FieldExtensions.SetStaticField<InputActionAsset, string>("Extension", value);
        return typeof(InputActionAsset);
    }

}
