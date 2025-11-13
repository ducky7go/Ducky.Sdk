using System;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class CraftingFormulaCollectionExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<CraftingFormula> GetList(this CraftingFormulaCollection target) =>
        target.GetField<CraftingFormulaCollection, List<CraftingFormula>>("list");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CraftingFormulaCollection
        SetList(this CraftingFormulaCollection target, List<CraftingFormula> value) =>
        target.SetField("list", value);
}
