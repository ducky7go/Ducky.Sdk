#nullable disable

using System;
using System.IO;

/// <summary>
/// Hot reload builds only (<see cref="BuildContext.HotReloadEnabled"/>): removes the Core files of earlier builds of
/// this mod from the output folder and the deployed mod folder, so exactly one Core remains (the one this build
/// made). A file the game holds open cannot be deleted; that is a warning and the build goes on, because the shim
/// picks the newest Core by stamp. Default builds skip the step.
/// </summary>
internal static class HotReloadCleanupStep
{
    internal static int Execute(BuildContext context)
    {
        if (!context.HotReloadEnabled || string.IsNullOrEmpty(context.ModName))
        {
            return SkipExitCode;
        }

        var keep = context.CoreAssemblyName;
        var outputDir = Path.Combine(context.ProjectDir, "bin", context.Configuration ?? "Debug",
            context.TargetFramework ?? "netstandard2.1");
        var deployDir = ResetDeployLib.GetTargetModDirectory(context);

        var removed = 0;
        foreach (var dir in new[] { outputDir, deployDir })
        {
            removed += RemoveOlderCores(context, dir, keep);
        }

        context.LogInfo($"[HotReload] Removed {removed} older Core file(s); kept {keep}.dll");
        return Program.ExitSuccess;
    }

    /// <summary>Deletes the Core assemblies (and PDBs) of <paramref name="modName"/> in <paramref name="dir"/> except <paramref name="keepName"/>.</summary>
    private static int RemoveOlderCores(BuildContext context, string dir, string keepName)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            return 0;
        }

        var removed = 0;
        var keepFile = keepName + ".dll";
        foreach (var file in Directory.GetFiles(dir, $"{context.ModName}.Core.*.dll"))
        {
            var fileName = Path.GetFileName(file);
            if (!HotReloadFiles.IsCoreFileOf(context.ModName, fileName) ||
                string.Equals(fileName, keepFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                File.Delete(file);
                var pdb = Path.ChangeExtension(file, ".pdb");
                if (File.Exists(pdb))
                {
                    File.Delete(pdb);
                }

                removed++;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                context.LogWarning($"[HotReload] Could not remove older Core {file} ({ex.Message}); the build continues and the shim loads the newest Core by stamp.");
            }
        }

        return removed;
    }
}
