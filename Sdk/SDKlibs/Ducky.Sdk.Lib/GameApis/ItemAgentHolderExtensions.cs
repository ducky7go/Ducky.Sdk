using System;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class ItemAgentHolderExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterMainControl GetCharacterController(this ItemAgentHolder target) =>
        target.GetField<ItemAgentHolder, CharacterMainControl>("characterController");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ItemAgentHolder SetCharacterController(this ItemAgentHolder target, CharacterMainControl value) =>
        target.SetField("characterController", value);
}
