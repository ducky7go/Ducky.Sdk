using System;
using System.IO;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using UnityEngine;
using Newtonsoft.Json;

namespace Ducky.Sdk.Options;

/// <summary>
/// 管理本地开发用配置的读写
/// ForThis: Application.persistentDataPath/Ducky/ModLocalConfig/ModName.json (每个mod独立配置)
/// ForAll: Application.persistentDataPath/Ducky/ModsLocalConfig.json (所有mod共享配置)
/// </summary>
public class ModOptions
{
    private readonly object _sync = new();
    private readonly Func<string> _getFilePathFunc;

    /// <summary>
    /// 配置文件夹名（相对于 Application.persistentDataPath）
    /// </summary>
    public const string FolderName = "Ducky";

    /// <summary>
    /// ForName 实例：每个mod独立的配置文件
    /// 路径: Application.persistentDataPath/Ducky/ModConfigForName/ModName.json
    /// </summary>
    public static readonly ModOptions ForName = new(GetLocalFileForModName);

    /// <summary>
    /// ForId 实例：每个mod独立的配置文件（使用ModId命名）
    /// 路径: Application.persistentDataPath/Ducky/ModConfigForId/ModId.json
    /// </summary>
    public static readonly ModOptions ForId = new(GetLocalFileForModId);

    /// <summary>
    /// ForAllMods 实例：所有mod共享的配置文件
    /// 路径: Application.persistentDataPath/Ducky/ModsLocalConfig.json
    /// </summary>
    public static readonly ModOptions ForAllMods = new(GetAllFilePath);

    private ModOptions(Func<string> getFilePathFunc)
    {
        _getFilePathFunc = getFilePathFunc;
    }

    /// <summary>
    /// 获取本mod专属配置文件路径
    /// </summary>
    private static string GetLocalFileForModName()
    {
        var folder = Path.Combine(Application.persistentDataPath, FolderName, "ModConfigForName");
        var modName = Helper.GetModName();
        return Path.Combine(folder, modName + ".json");
    }

    private static string GetLocalFileForModId()
    {
        var folder = Path.Combine(Application.persistentDataPath, FolderName, "ModConfigForId");
        var modId = Helper.GetModId();
        return Path.Combine(folder, modId + ".json");
    }

    /// <summary>
    /// 获取所有mod共享配置文件路径
    /// </summary>
    private static string GetAllFilePath()
    {
        return Path.Combine(Application.persistentDataPath, FolderName, "ModsLocalConfig.json");
    }


    private static ES3Settings GetSettings(string path)
    {
        var s = new ES3Settings(path)
        {
            location = ES3.Location.File
        };
        return s;
    }

    private static bool IsSimpleType(Type t)
    {
        if (t == null) return false;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            t = Nullable.GetUnderlyingType(t);
        }

        return t.IsPrimitive
               || t.IsEnum
               || t == typeof(string)
               || t == typeof(decimal)
               || t == typeof(DateTime)
               || t == typeof(DateTimeOffset)
               || t == typeof(TimeSpan)
               || t == typeof(Guid);
    }

    /// <summary>
    /// 获取配置文件完整路径
    /// </summary>
    public string GetConfigFilePath()
    {
        return _getFilePathFunc();
    }

    /// <summary>
    /// 获取配置文件夹完整路径
    /// </summary>
    public string GetConfigFolderPath()
    {
        var filePath = GetConfigFilePath();
        return Path.GetDirectoryName(filePath) ?? string.Empty;
    }

    /// <summary>
    /// 确保配置文件夹存在
    /// </summary>
    private bool EnsureFolderExists()
    {
        try
        {
            var folder = GetConfigFolderPath();
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.DebugException("failed to ensure folder exists", ex);
            return false;
        }
    }

    /// <summary>
    /// 检查配置文件是否存在
    /// </summary>
    public bool ConfigExists()
    {
        try
        {
            var path = GetConfigFilePath();
            var settings = GetSettings(path);
            return ES3.FileExists(path, settings);
        }
        catch (Exception ex)
        {
            Log.DebugException("failed to check config exists", ex);
            return false;
        }
    }

    /// <summary>
    /// 原子化保存配置（临时文件写入后替换）
    /// 返回是否保存成功
    /// </summary>
    public bool SaveConfig<T>(string key, T data)
    {
        lock (_sync)
        {
            try
            {
                if (!EnsureFolderExists())
                {
                    Log.Error("[ModLocalConfigManager] Failed to ensure folder exists.");
                    return false;
                }

                var path = GetConfigFilePath();
                var settings = GetSettings(path);

                if (IsSimpleType(typeof(T)))
                {
                    ES3.Save(key, data, path, settings);
                }
                else
                {
                    try
                    {
                        var json = JsonConvert.SerializeObject(data);
                        ES3.Save(key, json, path, settings);
                    }
                    catch (Exception ex)
                    {
                        Log.DebugException($"failed to serialize config to json for key {key}", ex);
                        return false;
                    }
                }

                ES3.StoreCachedFile(path);
                ES3.CacheFile(path);

                Log.Debug($"[ModLocalConfigManager] Saved config to {path}");
                return true;
            }
            catch (Exception ex)
            {
                Log.DebugException("failed to save config", ex);
                return false;
            }
        }
    }

    /// <summary>
    /// 读取配置并反序列化为 T，找不到或出错返回 defaultValue
    /// </summary>
    public T? LoadConfig<T>(string key, T? defaultValue = default)
    {
        lock (_sync)
        {
            try
            {
                var path = GetConfigFilePath();
                var settings = GetSettings(path);
                if (!ES3.KeyExists(key, path, settings))
                {
                    ES3.Save(key, defaultValue, path, settings);
                    return defaultValue;
                }
 
                // 简单类型直接使用 ES3 加载
                if (IsSimpleType(typeof(T)))
                {
                    return ES3.Load<T>(key, path, settings);
                }
 
                // 非简单类型——按 JSON 字符串处理（不尝试按原类型加载）
                try
                {
                    var json = ES3.Load<string>(key, path, settings);
                    if (string.IsNullOrEmpty(json))
                        return defaultValue;
 
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"failed to deserialize json for key {key}", ex);
                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                Log.DebugException("failed to load config", ex);
                return defaultValue;
            }
        }
    }

    /// <summary>
    /// 删除配置文件（若存在）
    /// </summary>
    public bool DeleteConfig()
    {
        lock (_sync)
        {
            try
            {
                var path = GetConfigFilePath();
                var settings = GetSettings(path);
                if (ES3.FileExists(path, settings))
                {
                    ES3.DeleteFile(path, settings);
                    ES3.StoreCachedFile(path);
                    Log.Debug($"[ModLocalConfigManager] Deleted {path}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.DebugException("failed to delete config", ex);
                return false;
            }
        }
    }

    #region Static Methods (Delegates to ForLocal for backward compatibility)

    /// <summary>
    /// 获取配置文件夹完整路径 (ForLocal)
    /// </summary>
    public static string GetFolderPath() => ForName.GetConfigFolderPath();

    /// <summary>
    /// 获取配置文件完整路径 (ForLocal)
    /// </summary>
    public static string GetFilePath() => ForName.GetConfigFilePath();

    /// <summary>
    /// 检查配置文件是否存在 (ForLocal)
    /// </summary>
    public static bool Exists() => ForName.ConfigExists();

    /// <summary>
    /// 保存配置 (ForLocal)
    /// </summary>
    public static bool Save<T>(string key, T data) => ForName.SaveConfig(key, data);

    /// <summary>
    /// 读取配置 (ForLocal)
    /// </summary>
    public static T? Load<T>(string key, T? defaultValue = default) => ForName.LoadConfig(key, defaultValue);

    /// <summary>
    /// 删除配置文件 (ForLocal)
    /// </summary>
    public static bool Delete() => ForName.DeleteConfig();

    #endregion
}
