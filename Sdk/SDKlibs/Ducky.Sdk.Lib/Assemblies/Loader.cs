using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ducky.Sdk.Logging;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace Ducky.Sdk.Assemblies;

/// <summary>
/// Centralized loader that exposes LoadHarmony and LoadR3 and reuses shared helpers
/// Provides a generic EnsureAssembliesLoaded helper so callers can pass file and name candidates.
/// </summary>
public static class Loader
{
    /// <summary>
    /// Load harmony using generic ensure helper.
    /// </summary>
    public static Assembly LoadHarmony()
    {
        var fileCandidates = new[] { "0Harmony.dll" };
        var assemblyNameCandidates = new[] { "0Harmony", "HarmonyLib" };
        var preferOrder = new[] { "0Harmony", "HarmonyLib" };

        return EnsureAssembliesLoaded(
            fileCandidates,
            assemblyNameCandidates,
            preferOrder,
            "[Loader/Harmony]",
            // additional type candidates to resolve via Type.GetType
            [
                "HarmonyLib.Harmony, 0Harmony",
                "HarmonyLib.Harmony, HarmonyLib",
                "Harmony.Harmony, 0Harmony",
                "Harmony.Harmony, HarmonyLib"
            ]);
    }

    /// <summary>
    /// Load R3 using generic ensure helper.
    /// </summary>
    public static Assembly LoadR3()
    {
        var fileCandidates = new[]
        {
            "Microsoft.Bcl.TimeProvider.dll",
            "ObservableCollections.dll",
            "ObservableCollections.R3.dll",
            "R3.dll"
        };
        var assemblyNameCandidates = new[]
        {
            "Microsoft.Bcl.TimeProvider",
            "ObservableCollections.R3",
            "ObservableCollections",
            "R3"
        };
        var preferOrder = new[]
        {
            "Microsoft.Bcl.TimeProvider",
            "ObservableCollections",
            "R3",
            "ObservableCollections.R3"
        };

        return EnsureAssembliesLoaded(
            fileCandidates,
            assemblyNameCandidates,
            preferOrder,
            "[Loader/R3]");
    }

    public static Assembly LoadSerilog()
    {
        var fileCandidates = new[]
        {
            "Serilog.dll",
            "Serilog.Sinks.File.dll",
        };
        var assemblyNameCandidates = new[]
        {
            "Serilog",
            "Serilog.Sinks.File",
        };
        var preferOrder = new[]
        {
            "Serilog",
            "Serilog.Sinks.File",
        };

        return EnsureAssembliesLoaded(
            fileCandidates,
            assemblyNameCandidates,
            preferOrder,
            "[Loader/Serilog]");
    }

    /// <summary>
    /// Generic helper: ensure one of the assemblies described by candidates is loaded.
    /// - fileCandidates: list of dll filenames to try loading from executing directory
    /// - assemblyNameCandidates: assembly simple names to try Assembly.Load
    /// - preferOrder: order to prefer when selecting which loaded assembly to return
    /// - tag: log tag
    /// - typeCandidates: optional Type.GetType strings to try resolve assembly from a type
    /// Returns located Assembly or throws FileNotFoundException.
    /// </summary>
    private static Assembly EnsureAssembliesLoaded(
        string[] fileCandidates,
        string[] assemblyNameCandidates,
        string[] preferOrder,
        string tag,
        string[]? typeCandidates = null)
    {
        var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        // Check if any preferred assembly already present
        foreach (var name in assemblyNameCandidates)
        {
            var asm = domainAssemblies.FirstOrDefault(a =>
                string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase)
                || (a.GetName().Name ?? "").IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            if (asm != null)
            {
                Log.Debug($"{tag} Reusing loaded assembly: {asm.GetName().Name} (Location: {asm.Location})");
                return asm;
            }
        }

        var dir = GetDependencyFolder();

        // Try load from files first
        foreach (var file in fileCandidates)
        {
            var path = Path.Combine(dir, file);
            Log.Debug($"{tag} Checking file candidate: {path}");
            var asm = TryLoadFromFile(path, tag);
            if (asm != null)
            {
                domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                var preferred = FindPreferredAssembly(domainAssemblies, preferOrder);
                if (preferred != null) return preferred;
                return asm;
            }
        }

        // Try Assembly.Load by names
        foreach (var candidate in assemblyNameCandidates.Distinct())
        {
            var asm = TryLoadByName(candidate, tag);
            if (asm != null)
            {
                domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                var preferred = FindPreferredAssembly(domainAssemblies, preferOrder);
                if (preferred != null) return preferred;
                return asm;
            }
        }

        // Try resolving from type names if provided
        if (typeCandidates != null)
        {
            foreach (var tc in typeCandidates)
            {
                try
                {
                    var t = Type.GetType(tc);
                    if (t?.Assembly != null)
                    {
                        Log.Current.Debug(
                            $"{tag} Resolved assembly from type {tc}: {t.Assembly.GetName().Name} (Location: {t.Assembly.Location})");
                        return t.Assembly;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"{tag} Type.GetType(\"{tc}\") failed: {ex.Message}");
                }
            }
        }

        // If still nothing, attempt fallback: find any assembly containing prefer keywords
        domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var fallback = domainAssemblies.FirstOrDefault(a =>
        {
            var n = (a.GetName().Name ?? "").ToLowerInvariant();
            return preferOrder.Any(p => n.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                   || assemblyNameCandidates.Any(p => n.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0);
        });
        if (fallback != null)
        {
            Log.Debug($"{tag} Fallback assembly found: {fallback.GetName().Name} (Location: {fallback.Location})");
            return fallback;
        }

        Debug.LogError(
            $"{tag} Unable to locate assemblies. Directory: {dir}. Tried files: {string.Join(", ", fileCandidates)}; names: {string.Join(", ", assemblyNameCandidates)}");
        throw new FileNotFoundException(
            $"Unable to locate assemblies. Tried files: {string.Join(", ", fileCandidates)}; names: {string.Join(", ", assemblyNameCandidates)}");
    }

    private static string GetDependencyFolder()
    {
        var modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrEmpty(modFolder))
        {
            return Path.Combine(modFolder, "Dependency");
        }

        return string.Empty;
    }

    private static Assembly? TryLoadFromFile(string path, string tag)
    {
        try
        {
            if (File.Exists(path))
            {
                var asm = Assembly.LoadFrom(path);
                Log.Debug($"{tag} Loaded assembly from file: {asm.GetName().Name} (Location: {asm.Location})");
                return asm;
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"{tag} Failed to load assembly from file {path}: {ex}");
            return null;
        }
    }

    private static Assembly? TryLoadByName(string name, string tag)
    {
        try
        {
            Log.Debug($"{tag} Attempting Assembly.Load(\"{name}\")");
            var asm = Assembly.Load(new AssemblyName(name));
            if (asm != null)
            {
                Log.Debug($"{tag} Loaded assembly via Assembly.Load: {asm.GetName().Name} (Location: {asm.Location})");
                return asm;
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Debug($"{tag} Assembly.Load(\"{name}\") failed: {ex.Message}");
            return null;
        }
    }

    private static Assembly? FindPreferredAssembly(Assembly[] domainAssemblies, string[] preferOrder)
    {
        foreach (var name in preferOrder)
        {
            var asm = domainAssemblies.FirstOrDefault(a =>
                string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
            if (asm != null) return asm;
        }

        // also try contains match
        foreach (var name in preferOrder)
        {
            var asm = domainAssemblies.FirstOrDefault(a =>
                (a.GetName().Name ?? "").IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            if (asm != null) return asm;
        }

        return null;
    }

    public static void LoadDependenciesFromDepsJson()
    {
        var depFolder = GetDependencyFolder();
        var assemblyName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        var depsJsonPath = Path.Combine(depFolder, $"{assemblyName}.deps.json");
        LoadDependenciesFromDepsJson(depsJsonPath);
    }

    // 自动加载依赖项：解析 deps.json 并按顺序加载 DLL
    public static void LoadDependenciesFromDepsJson(string depsJsonPath)
    {
        if (!File.Exists(depsJsonPath))
        {
            Log.Debug($"[Loader/DepsJson] deps.json 文件不存在: {depsJsonPath}");
            return;
        }

        try
        {
            var json = File.ReadAllText(depsJsonPath);
            var root = JObject.Parse(json);

            // 解析 targets 节点
            var targets = root["targets"] as JObject;
            if (targets == null)
            {
                Log.Warn($"[Loader/DepsJson] 未找到 targets 节点");
                return;
            }

            // 取第一个 target（一般为 .NETStandard,Version=v2.1/）
            var target = targets.Properties().FirstOrDefault(x => x.Name == ".NETStandard,Version=v2.1/")?.Value as JObject;
            if (target == null)
            {
                Log.Warn($"[Loader/DepsJson] 未找到 .NETStandard,Version=v2.1/ 目标");
                return;
            }

            // 按 dependencies 顺序遍历
            foreach (var lib in target.Properties())
            {
                var libObj = lib.Value as JObject;
                if (libObj == null) continue;
                if (libObj["dependencies"] == null) continue;

                // 当前库的 runtime DLL
                var runtime = libObj["runtime"] as JObject;
                if (runtime != null)
                {
                    foreach (var dll in runtime.Properties())
                    {
                        var dllName = dll.Name;
                        // 检查是否已加载
                        var loaded = AppDomain.CurrentDomain.GetAssemblies()
                            .Any(a => Path.GetFileName(a.Location).Equals(dllName, StringComparison.OrdinalIgnoreCase));
                        if (loaded)
                        {
                            Log.Debug($"[Loader/DepsJson] 已加载: {dllName}");
                            continue;
                        }
                        // 依赖文件路径
                        var depFolder = GetDependencyFolder();
                        var dllPath = Path.Combine(depFolder, dllName);
                        if (File.Exists(dllPath))
                        {
                            try
                            {
                                Assembly.LoadFrom(dllPath);
                                Log.Debug($"[Loader/DepsJson] 加载依赖: {dllPath}");
                            }
                            catch (Exception ex)
                            {
                                Log.Debug($"[Loader/DepsJson] 加载失败: {dllPath} - {ex.Message}");
                            }
                        }
                        else
                        {
                            Log.Debug($"[Loader/DepsJson] 未找到 DLL: {dllPath}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Debug($"[Loader/DepsJson] 解析或加载失败: {ex.Message}");
        }
    }
}
