using System;
using System.Runtime.CompilerServices;
using Duckov.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class StatInfoDatabaseExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatInfoDatabase.Entry[] GetEntries(this StatInfoDatabase target) =>
        target.GetField<StatInfoDatabase, StatInfoDatabase.Entry[]>("entries");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StatInfoDatabase SetEntries(this StatInfoDatabase target, StatInfoDatabase.Entry[] value) =>
        target.SetField("entries", value);

}
