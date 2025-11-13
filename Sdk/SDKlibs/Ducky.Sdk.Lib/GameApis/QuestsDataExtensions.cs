using System;
using System.Runtime.CompilerServices;
using Duckov.Utilities;
using Duckov.Quests;
using Duckov.Quests.Relations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace Ducky.Sdk.GameApis;

// ReSharper disable once InconsistentNaming
public static class QuestsDataExtensions
{
    // Instance field accessors
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.QuestsData SetDefaultQuestGiverDisplayName(this GameplayDataSettings.QuestsData target, string value) =>
        target.SetField("defaultQuestGiverDisplayName", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.QuestsData SetQuestCollection(this GameplayDataSettings.QuestsData target, QuestCollection value) =>
        target.SetField("questCollection", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<GameplayDataSettings.QuestsData.QuestGiverInfo> GetQuestGiverInfos(this GameplayDataSettings.QuestsData target) =>
        target.GetField<GameplayDataSettings.QuestsData, List<GameplayDataSettings.QuestsData.QuestGiverInfo>>("questGiverInfos");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.QuestsData SetQuestGiverInfos(this GameplayDataSettings.QuestsData target, List<GameplayDataSettings.QuestsData.QuestGiverInfo> value) =>
        target.SetField("questGiverInfos", value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameplayDataSettings.QuestsData SetQuestRelation(this GameplayDataSettings.QuestsData target, QuestRelationGraph value) =>
        target.SetField("questRelation", value);

}
