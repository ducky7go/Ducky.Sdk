using System;
using System.Runtime.CompilerServices;
using ItemStatsSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class CharacterItemControlExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl GetCharacterMainControl(this CharacterItemControl target) =>
        target.GetField<CharacterItemControl, CharacterMainControl>("characterMainControl");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterItemControl SetCharacterMainControl(this CharacterItemControl target,
        CharacterMainControl value) =>
        target.SetField("characterMainControl", value);
}
