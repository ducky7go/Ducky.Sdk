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
    private readonly IModOptionsStorage _storage;

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

    internal ModOptions(Func<string> getFilePathFunc, IModOptionsStorage storage = null)
    {
        _getFilePathFunc = getFilePathFunc;
        _storage = storage ?? new Es3ModOptionsStorage();
        EnsureFolderExists();
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

    internal static ES3Settings CreateSettings(string path)
    {
        var s = new ES3Settings(path)
        {
            location = ES3.Location.File,
        };
        return s;
    }

    private static bool IsSimpleType(Type t)
    {
        if (t == null) return false;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            // 处理可空时间类型
            var underlyingType = Nullable.GetUnderlyingType(t);
            return underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset);
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
    /// 检查是否为时间相关类型（DateTime、DateTimeOffset及其可空版本）
    /// </summary>
    internal static bool IsDateTimeType(Type t)
    {
        if (t == null) return false;

        // 检查非空类型
        if (t == typeof(DateTime) || t == typeof(DateTimeOffset))
            return true;

        // 检查可空类型
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(t);
            return underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset);
        }

        return false;
    }

    /// <summary>
    /// 将时间类型转换为Unix时间戳（秒）
    /// </summary>
    internal static long ConvertToUnixTimestamp(object value)
    {
        if (value == null) return 0;

        if (value is DateTime dateTime)
            return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();

        if (value is DateTimeOffset dateTimeOffset)
            return dateTimeOffset.ToUnixTimeSeconds();

        return 0;
    }

    /// <summary>
    /// 将Unix时间戳转换为时间类型
    /// </summary>
    internal static object ConvertFromUnixTimestamp(Type targetType, long unixTimestamp)
    {
        // 值为0时返回null（对于可空类型）或DateTime/DateTimeOffset.MinValue（对于非空类型）
        if (unixTimestamp == 0)
        {
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return null;

            if (targetType == typeof(DateTime))
                return DateTime.MinValue;

            if (targetType == typeof(DateTimeOffset))
                return DateTimeOffset.MinValue;
        }

        var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);

        if (targetType == typeof(DateTime) ||
            (targetType.IsGenericType && Nullable.GetUnderlyingType(targetType) == typeof(DateTime)))
            return dateTimeOffset.DateTime;

        return dateTimeOffset;
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
            return _storage.FileExists(path);
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
                if (IsSimpleType(typeof(T)))
                {
                    // 处理时间类型
                    if (IsDateTimeType(typeof(T)))
                    {
                        var unixTimestamp = ConvertToUnixTimestamp(data);
                        _storage.Save(key, unixTimestamp, path);
                    }
                    else
                    {
                        _storage.Save(key, data, path);
                    }
                }
                else
                {
                    try
                    {
                        var json = JsonConvert.SerializeObject(data);
                        _storage.Save(key, json, path);
                    }
                    catch (Exception ex)
                    {
                        Log.DebugException($"failed to serialize config to json for key {key}", ex);
                        return false;
                    }
                }

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

                if (!_storage.KeyExists(key, path))
                {
                    _storage.Save(key, defaultValue, path);
                    return defaultValue;
                }

                // 简单类型直接使用 ES3 加载
                if (IsSimpleType(typeof(T)))
                {
                    // 处理时间类型
                    if (IsDateTimeType(typeof(T)))
                    {
                        try
                        {
                            // 尝试加载为long类型
                            var unixTimestamp = _storage.Load<long>(key, path);
                            return (T)ConvertFromUnixTimestamp(typeof(T), unixTimestamp);
                        }
                        catch (Exception ex)
                        {
                            // 任何异常都记录日志并返回默认值
                            Log.DebugException($"尝试加载时间类型配置失败，键名: {key}，将返回默认值", ex);
                            return defaultValue;
                        }
                    }

                    return _storage.Load<T>(key, path);
                }

                // 非简单类型——按 JSON 字符串处理（不尝试按原类型加载）
                try
                {
                    var json = _storage.Load<string>(key, path);
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
                if (_storage.FileExists(path))
                {
                    _storage.DeleteFile(path);
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

internal interface IModOptionsStorage
{
    bool FileExists(string path);
    bool KeyExists(string key, string path);
    void Save<T>(string key, T data, string path);
    T Load<T>(string key, string path);
    void DeleteFile(string path);
}

internal sealed class Es3ModOptionsStorage : IModOptionsStorage
{
    public bool FileExists(string path)
    {
        var settings = ModOptions.CreateSettings(path);
        return ES3.FileExists(path, settings);
    }

    public bool KeyExists(string key, string path)
    {
        var settings = ModOptions.CreateSettings(path);
        return ES3.KeyExists(key, path, settings);
    }

    public void Save<T>(string key, T data, string path)
    {
        var settings = ModOptions.CreateSettings(path);
        ES3.Save(key, data, path, settings);
    }

    public T Load<T>(string key, string path)
    {
        var settings = ModOptions.CreateSettings(path);
        return ES3.Load<T>(key, path, settings);
    }

    public void DeleteFile(string path)
    {
        var settings = ModOptions.CreateSettings(path);
        ES3.DeleteFile(path, settings);
    }
}
