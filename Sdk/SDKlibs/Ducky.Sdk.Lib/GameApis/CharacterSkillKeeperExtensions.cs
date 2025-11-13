using System;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class CharacterSkillKeeperExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Action GetOnSkillChanged(this CharacterSkillKeeper target) =>
        target.GetField<CharacterSkillKeeper, Action>("OnSkillChanged");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterSkillKeeper SetOnSkillChanged(this CharacterSkillKeeper target, Action value) =>
        target.SetField("OnSkillChanged", value);

}
