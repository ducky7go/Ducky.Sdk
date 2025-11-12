using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Duckov.Buffs;
using Duckov.Utilities;
using Ducky.Sdk.GameApis;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Options;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.Sdk.Contracts;

public class BuffsContract : MonoBehaviour
{
    private const int BuffIdRegionSize = 10_000;
    private const int StartingBuffId = 1500000;
    private const string BuffRegionOptionKey = "BuffIdRegion";

    static BuffsContract()
    {
        var modId = Helper.GetModId();
        Log.Info($"Initializing BuffsContract for mod {modId}.");
        var objName = $"{modId}_BuffsContractHolder";
        var existing = GameObject.Find(objName);
        if (existing != null)
        {
            Instance = existing.GetComponent<BuffsContract>();
            if (Instance != null)
            {
                Log.Info($"BuffsContract instance for mod {modId} already exists.");
                return;
            }
        }

        var go = new GameObject(objName);
        Instance = go.AddComponent<BuffsContract>();
        DontDestroyOnLoad(go);
    }

    public static BuffsContract Instance { get; }

    private static readonly ConcurrentDictionary<Type, Buff> Buffs = new();
    private static int _maxBuffId = 1000456;

    public int RegisterBuff<T>(Action<T> configure) where T : Buff
    {
        var buffType = typeof(T);
        if (Buffs.TryGetValue(buffType, out var old))
        {
            Debug.LogWarning($"Buff of type {buffType.FullName} is already registered.");
            return old.ID;
        }

        var obj = new GameObject($"BuffRegistrar_{buffType.Name}");
        DontDestroyOnLoad(obj);
        var buff = obj.AddComponent<T>();
        buff.ID = _maxBuffId++;
        configure(buff);

        Buffs[buffType] = buff;
        var allBuffs = GameplayDataSettings.Buffs.GetAllBuffs();
        allBuffs.Add(buff);
        Log.Info($"Registered buff of type {buffType.FullName} with ID {buff.ID}.");
        return buff.ID;
    }

    public Buff? CreateBuffInstance(int id)
    {
        var allBuffs = GameplayDataSettings.Buffs.GetAllBuffs()!;
        var prefInstance = allBuffs.FirstOrDefault(b => b.ID == id);
        if (prefInstance == null)
        {
            Log.WarnFormat("Buff with ID {0} not found.", id);
            return null;
        }

        var buffInstance = Instantiate(prefInstance);
        return buffInstance;
    }

    private static readonly Dictionary<string, int> Default = new();

    /// <summary>
    /// ensure buff id region for current mod, this must be called before registering any buffs
    /// </summary>
    public void EnsureBuffIdRegion()
    {
        var currentModId = Helper.GetModId();
        var regions = ModOptions.ForAllMods.LoadConfig(BuffRegionOptionKey, Default)!;
        if (!regions.TryGetValue(currentModId, out var value))
        {
            var newRegionStart = StartingBuffId + regions.Count * BuffIdRegionSize;
            regions[currentModId] = newRegionStart;
            ModOptions.ForAllMods.SaveConfig(BuffRegionOptionKey, regions);
            _maxBuffId = newRegionStart;
            Log.Info($"Assigned new buff ID region for mod {currentModId} starting at {_maxBuffId}.");
        }
        else
        {
            _maxBuffId = value;
            Log.Info($"Loaded buff ID region for mod {currentModId} starting at {_maxBuffId}.");
        }
    }
}
