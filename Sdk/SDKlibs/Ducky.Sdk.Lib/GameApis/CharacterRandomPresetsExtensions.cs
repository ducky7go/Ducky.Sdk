using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class CharacterRandomPresetsExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<CharacterRandomPreset> GetPresets(this GameplayDataSettings.CharacterRandomPresets target) =>
        target.GetField<GameplayDataSettings.CharacterRandomPresets, List<CharacterRandomPreset>>("presets");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.CharacterRandomPresets SetPresets(
        this GameplayDataSettings.CharacterRandomPresets target, List<CharacterRandomPreset> value) =>
        target.SetField("presets", value);
}
