using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;
using Ducky.Sdk.GameApis;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class StringListsDataExtensions
{
    // Static field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringList GetStaticItemAgentKeys() =>
        FieldExtensions.GetStaticField<GameplayDataSettings.StringListsData, StringList>("ItemAgentKeys");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticItemAgentKeys(StringList value)
    {
        FieldExtensions.SetStaticField<GameplayDataSettings.StringListsData, StringList>("ItemAgentKeys", value);
        return typeof(GameplayDataSettings.StringListsData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringList GetStaticSlotTypes() =>
        FieldExtensions.GetStaticField<GameplayDataSettings.StringListsData, StringList>("SlotTypes");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticSlotTypes(StringList value)
    {
        FieldExtensions.SetStaticField<GameplayDataSettings.StringListsData, StringList>("SlotTypes", value);
        return typeof(GameplayDataSettings.StringListsData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringList GetStaticStatKeys() =>
        FieldExtensions.GetStaticField<GameplayDataSettings.StringListsData, StringList>("StatKeys");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type SetStaticStatKeys(StringList value)
    {
        FieldExtensions.SetStaticField<GameplayDataSettings.StringListsData, StringList>("StatKeys", value);
        return typeof(GameplayDataSettings.StringListsData);
    }

}
