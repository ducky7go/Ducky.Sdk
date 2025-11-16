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

    internal static string GetModName()
    {
        if (!string.IsNullOrEmpty(_modName))
        {
            Debug.Log($"[Helper] Returning cached mod name: {_modName}");
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
                _modName = asm.GetName().Name ?? "Mod";
            }
        }
        catch
        {
            _modName = Assembly.GetExecutingAssembly().GetName().Name ?? "Mod";
        }

        Debug.Log($"[Helper] Returning cached mod name: {_modName}");
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

        var asm = Assembly.GetExecutingAssembly();
        var location = asm.Location;
        var folderName = Path.GetFileName(Path.GetDirectoryName(location));
        
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
}
