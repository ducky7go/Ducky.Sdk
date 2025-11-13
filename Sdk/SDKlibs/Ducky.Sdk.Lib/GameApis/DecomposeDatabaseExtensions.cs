using System;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class DecomposeDatabaseExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecomposeFormula[] GetEntries(this DecomposeDatabase target) =>
        target.GetField<DecomposeDatabase, DecomposeFormula[]>("entries");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecomposeDatabase SetEntries(this DecomposeDatabase target, DecomposeFormula[] value) =>
        target.SetField("entries", value);
}
