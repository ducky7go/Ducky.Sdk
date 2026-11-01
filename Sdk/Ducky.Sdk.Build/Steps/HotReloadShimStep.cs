#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Hot reload builds only. The shim is the assembly the game loads as ModName.dll: it loads the newest Core from bytes
/// and reloads it while the marker file exists. This step has two halves:
/// <list type="bullet">
/// <item><see cref="PrepareSource"/> (pre phase): writes the shim source, the SDK template with ModName substituted.
/// The MSBuild target DuckyHotReloadShimCompile compiles it after the pre phase.</item>
/// <item><see cref="Deploy"/> (post phase): copies the compiled shim to the mod folder, copies 0Harmony.dll beside it
/// (only when its content differs, so it stays one unchanged file across builds), and writes hotreload.enabled.</item>
/// </list>
/// Default builds skip both halves.
/// </summary>
internal static class HotReloadShimStep
{
    /// <summary>Placeholder in Shim.cs.template that is replaced with the mod name.</summary>
    internal const string ModNamePlaceholder = "__MOD_NAME__";

    internal static int PrepareSource(BuildContext context)
    {
        if (!context.HotReloadEnabled)
        {
            return SkipExitCode;
        }

        var template = context.ShimTemplatePath;
        if (string.IsNullOrEmpty(template) || !File.Exists(template))
        {
            Console.Error.WriteLine($"[HotReload][ERROR] Shim template not found: {template ?? "(not set)"}. " +
                                    "Reinstall or restore the Ducky.Sdk package, then rebuild.");
            return Program.ExitFailure;
        }

        var source = File.ReadAllText(template, Encoding.UTF8).Replace(ModNamePlaceholder, context.ModName);
        var sourcePath = HotReloadFiles.ShimSourcePath(context);
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath));
        File.WriteAllText(sourcePath, source, new UTF8Encoding(false));
        context.LogInfo($"[HotReload] Shim source written: {sourcePath}");
        return Program.ExitSuccess;
    }

    internal static int Deploy(BuildContext context)
    {
        if (!context.HotReloadEnabled || !context.DeployMod || context.IsModLib)
        {
            return SkipExitCode;
        }

        var shim = HotReloadFiles.ShimAssemblyPath(context);
        if (!File.Exists(shim))
        {
            Console.Error.WriteLine($"[HotReload][ERROR] Compiled shim not found: {shim}. " +
                                    "The DuckyHotReloadShimCompile target must run before the post phase.");
            return Program.ExitFailure;
        }

        var modDir = ResetDeployLib.GetTargetModDirectory(context);
        Directory.CreateDirectory(modDir);

        File.Copy(shim, Path.Combine(modDir, $"{context.ModName}.dll"), true);

        if (!string.IsNullOrEmpty(context.HarmonyDllPath) && File.Exists(context.HarmonyDllPath))
        {
            CopyIfDifferent(context.HarmonyDllPath, Path.Combine(modDir, HotReloadFiles.HarmonyFileName), context);
        }
        else
        {
            context.LogWarning("[HotReload] 0Harmony.dll not found; Core loads that need Harmony cannot resolve it.");
        }

        File.WriteAllBytes(Path.Combine(modDir, HotReloadFiles.MarkerFileName), Array.Empty<byte>());
        context.LogInfo($"[HotReload] Shim, Harmony and marker deployed to: {modDir}");
        return Program.ExitSuccess;
    }

    /// <summary>The files Deploy writes in hot reload builds, for DeployManifest. Empty in default builds.</summary>
    internal static List<string> PlannedOutputs(BuildContext context)
    {
        var planned = new List<string>();
        if (!context.HotReloadEnabled || !context.DeployMod || context.IsModLib)
        {
            return planned;
        }

        var modDir = ResetDeployLib.GetTargetModDirectory(context);
        planned.Add(Path.Combine(modDir, $"{context.ModName}.dll"));
        planned.Add(Path.Combine(modDir, HotReloadFiles.MarkerFileName));
        if (!string.IsNullOrEmpty(context.HarmonyDllPath) && File.Exists(context.HarmonyDllPath))
        {
            planned.Add(Path.Combine(modDir, HotReloadFiles.HarmonyFileName));
        }

        return planned;
    }

    /// <summary>Copies only when the destination is missing or its bytes differ, so an unchanged file keeps its contents.</summary>
    internal static bool CopyIfDifferent(string source, string destination, BuildContext context)
    {
        if (File.Exists(destination) && SameContent(source, destination))
        {
            context.LogInfo($"[HotReload] Unchanged: {destination}");
            return false;
        }

        File.Copy(source, destination, true);
        context.LogInfo($"[HotReload] Copied: {destination}");
        return true;
    }

    private static bool SameContent(string a, string b)
    {
        var infoA = new FileInfo(a);
        var infoB = new FileInfo(b);
        if (infoA.Length != infoB.Length)
        {
            return false;
        }

        using var sha = SHA256.Create();
        using var streamA = File.OpenRead(a);
        using var streamB = File.OpenRead(b);
        return sha.ComputeHash(streamA).AsSpan().SequenceEqual(sha.ComputeHash(streamB));
    }
}
