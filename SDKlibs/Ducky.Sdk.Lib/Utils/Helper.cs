using System;
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
        // mod in Mods folder prefix with local
        // mod in Workshop folder prefix with steam
        // otherwise return ModName as id
        if (!string.IsNullOrEmpty(_modId))
        {
            return _modId!;
        }

        var asm = Assembly.GetExecutingAssembly();
        var location = asm.Location;
        if (location.Contains("/Mods/", StringComparison.OrdinalIgnoreCase))
        {
            var folderName = location.Split(new[] { "/Mods/" }, StringSplitOptions.None)[1]
                .Split(['/'], StringSplitOptions.None)[0];
            _modId = "local." + folderName;
        }

        else if (location.Contains("/Workshop/", StringComparison.OrdinalIgnoreCase))
        {
            var folderName = location.Split(new[] { "/Workshop/" }, StringSplitOptions.None)[1]
                .Split(['/'], StringSplitOptions.None)[0];
            _modId = "steam." + folderName;
        }
        else
        {
            _modId = GetModName();
        }

        return _modId!;
    }
}