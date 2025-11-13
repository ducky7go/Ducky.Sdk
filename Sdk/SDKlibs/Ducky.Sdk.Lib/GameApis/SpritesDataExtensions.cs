using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class SpritesDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<GameplayDataSettings.SpritesData.Entry> GetEntries(this GameplayDataSettings.SpritesData target) =>
        target.GetField<GameplayDataSettings.SpritesData, List<GameplayDataSettings.SpritesData.Entry>>("entries");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.SpritesData SetEntries(this GameplayDataSettings.SpritesData target, List<GameplayDataSettings.SpritesData.Entry> value) =>
        target.SetField("entries", value);

}
