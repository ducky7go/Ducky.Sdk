using System;
using System.Runtime.CompilerServices;
using Duckov.Buffs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class CharacterBuffManagerExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CharacterBuffManager SetMaster(this CharacterBuffManager target, CharacterMainControl value) =>
        target.SetField("master", value);

}
