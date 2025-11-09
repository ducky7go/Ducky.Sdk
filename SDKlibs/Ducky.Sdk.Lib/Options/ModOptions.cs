using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ducky.Sdk.Attributes;
using Ducky.Sdk.Logging;
using Ducky.Sdk.Utils;
using UnityEngine;

namespace Ducky.Sdk.Configurations;

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
    /// ForThis 实例：每个mod独立的配置文件
    /// 路径: Application.persistentDataPath/Ducky/ModLocalConfig/ModName.json
    /// </summary>
    public static readonly ModOptions ForThis = new(GetLocalFilePath);

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
    private static string GetLocalFilePath()
    {
        var folder = Path.Combine(Application.persistentDataPath, FolderName, "ModLocalConfig");
        var modName = Helper.GetModName();
        return Path.Combine(folder, modName + ".json");
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

                ES3.Save(key, data, path, settings);
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

                return ES3.Load<T>(key, path, settings);
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
    public static string GetFolderPath() => ForThis.GetConfigFolderPath();

    /// <summary>
    /// 获取配置文件完整路径 (ForLocal)
    /// </summary>
    public static string GetFilePath() => ForThis.GetConfigFilePath();

    /// <summary>
    /// 检查配置文件是否存在 (ForLocal)
    /// </summary>
    public static bool Exists() => ForThis.ConfigExists();

    /// <summary>
    /// 保存配置 (ForLocal)
    /// </summary>
    public static bool Save<T>(string key, T data) => ForThis.SaveConfig(key, data);

    /// <summary>
    /// 读取配置 (ForLocal)
    /// </summary>
    public static T? Load<T>(string key, T? defaultValue = default) => ForThis.LoadConfig(key, defaultValue);

    /// <summary>
    /// 删除配置文件 (ForLocal)
    /// </summary>
    public static bool Delete() => ForThis.DeleteConfig();

    #endregion
}