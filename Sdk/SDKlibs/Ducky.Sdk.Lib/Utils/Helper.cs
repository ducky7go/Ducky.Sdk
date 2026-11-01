using System;
using System.Collections.Generic;
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

        _modId = ResolveModId(GetModFolderName(), GetModName());
        return _modId!;
    }

    /// <summary>
    /// The mod id for a folder name: "steam.&lt;digits&gt;" for a Workshop folder, "local.&lt;name&gt;" otherwise,
    /// and the mod name when no folder is known. Pure, so it is unit-tested without Unity.
    /// </summary>
    internal static string ResolveModId(string? folderName, string modName)
    {
        if (string.IsNullOrEmpty(folderName))
        {
            return modName;
        }

        // Check if folder name is all digits (Steam Workshop mod)
        return folderName.All(char.IsDigit) ? "steam." + folderName : "local." + folderName;
    }

    /// <summary>
    /// Name of the folder that holds the running mod. A Core loaded from bytes (hot reload) has no
    /// <see cref="Assembly.Location"/>, so the folder comes from the already loaded mod assembly of the same mod.
    /// Returns null when neither location is known; it never throws.
    /// </summary>
    internal static string? GetModFolderName()
    {
        var directory = GetModFolderPath();
        return string.IsNullOrEmpty(directory) ? null : Path.GetFileName(directory);
    }

    private static string? _modFolderPath;

    /// <summary>
    /// Full path of the folder that holds the running mod, or null when it is unknown. It never throws. Only a
    /// non-null result is cached, so a shim loaded later is still found on a later call.
    /// </summary>
    internal static string? GetModFolderPath()
    {
        if (!string.IsNullOrEmpty(_modFolderPath))
        {
            return _modFolderPath;
        }

        try
        {
            var self = Assembly.GetExecutingAssembly();
            var path = ResolveModFolderPath(
                self.GetName().Name,
                () => self.Location,
                AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => ((Func<string?>)(() => a.GetName().Name), (Func<string?>)(() => a.Location))));
            if (!string.IsNullOrEmpty(path))
            {
                _modFolderPath = path;
            }

            return path;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Folder of the running mod given the running assembly's name and location, and the loaded assemblies as
    /// (name, location) readers. Falls back to the loaded shim of the same mod when the running assembly has no
    /// location. A reader that throws <see cref="NotSupportedException"/> (a dynamic assembly) is skipped, so one
    /// such assembly cannot hide the shim. Pure, so it is unit-tested without Unity.
    /// </summary>
    internal static string? ResolveModFolderPath(
        string? ownAssemblyName,
        Func<string?> ownLocation,
        IEnumerable<(Func<string?> Name, Func<string?> Location)> loadedAssemblies)
    {
        var location = ReadOrNull(ownLocation);
        if (string.IsNullOrEmpty(location))
        {
            var modName = HotReloadNames.ToModName(ownAssemblyName ?? string.Empty);
            foreach (var (name, loadedLocation) in loadedAssemblies)
            {
                if (ReadOrNull(name) != modName)
                {
                    continue;
                }

                location = ReadOrNull(loadedLocation);
                if (!string.IsNullOrEmpty(location))
                {
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(location))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(location);
        return string.IsNullOrEmpty(directory) ? null : directory;
    }

    private static string? ReadOrNull(Func<string?> read)
    {
        try
        {
            return read();
        }
        catch (Exception)
        {
            return null; // dynamic assembly (no location) or a reader that fails: skip it
        }
    }
}
