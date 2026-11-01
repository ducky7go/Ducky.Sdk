using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ducky.Sdk.Attributes;
using UnityEngine;

namespace Ducky.Sdk.Utils;

public class Helper
{
    private static string? _modName;
    private static int _modNameLogShown = 0;

    internal static string GetModName()
    {
        if (!string.IsNullOrEmpty(_modName))
        {
            if (_modNameLogShown == 0)
            {
                Debug.Log($"[Helper] Returning cached mod name: {_modName}");
                _modNameLogShown = 1;
            }

            return _modName!;
        }

        try
        {
            var asm = Assembly.GetExecutingAssembly();
            // get type from ModBehaviour
            foreach (var t in asm.GetTypes()
                         .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MonoBehaviour))))
            {
                var attr = t.GetCustomAttribute<ModNameAttribute>();
                if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
                {
                    _modName = attr.Name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(_modName))
            {
                _modName = HotReloadNames.ToModName(asm.GetName().Name ?? "Mod");
            }
        }
        catch
        {
            _modName = HotReloadNames.ToModName(Assembly.GetExecutingAssembly().GetName().Name ?? "Mod");
        }

        if (_modNameLogShown == 0)
        {
            Debug.Log($"[Helper] Returning cached mod name: {_modName}");
            _modNameLogShown = 1;
        }

        return _modName!;
    }

    private static string? _modId;

    /// <summary>
    ///  Get Mod Id from folder name
    /// </summary>
    /// <returns></returns>
    internal static ModId GetModId()
    {
        // check folder to get mod id
        // if folder name is all digits, prefix with steam (Workshop mod)
        // otherwise prefix with local (local mod)
        if (!string.IsNullOrEmpty(_modId))
        {
            return _modId!;
        }

        var folderName = GetModFolderName();

        if (!string.IsNullOrEmpty(folderName))
        {
            // Check if folder name is all digits (Steam Workshop mod)
            if (folderName.All(char.IsDigit))
            {
                _modId = "steam." + folderName;
            }
            else
            {
                _modId = "local." + folderName;
            }
        }
        else
        {
            _modId = GetModName();
        }

        return _modId!;
    }

    /// <summary>
    /// Name of the folder that holds the running mod. A Core loaded from bytes (hot reload) has no
    /// <see cref="Assembly.Location"/>, so the folder comes from the already loaded mod assembly of the same mod.
    /// Returns null when neither location is known; it never throws.
    /// </summary>
    internal static string? GetModFolderName()
    {
        try
        {
            var location = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(location))
            {
                location = FindLoadedModLocation();
            }

            if (string.IsNullOrEmpty(location))
            {
                return null;
            }

            var directory = Path.GetDirectoryName(location);
            return string.IsNullOrEmpty(directory) ? null : Path.GetFileName(directory);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? FindLoadedModLocation()
    {
        var modName = HotReloadNames.ToModName(Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetName().Name == modName && !string.IsNullOrEmpty(assembly.Location))
            {
                return assembly.Location;
            }
        }

        return null;
    }
}
