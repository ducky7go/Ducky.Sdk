#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Remembers which files the SDK writes into the mod folder, so ResetDeployLib can remove the files that a later
/// build no longer produces and leave every other file alone.
/// The manifest is obj/ducky-deployed-files.txt. Its first line is "modDir=&lt;full path&gt;", the folder the entries
/// belong to. Each following line is one file path relative to that folder, with forward slashes.
/// BuildRunner writes it at the end of a successful post phase. A missing manifest, or one for another folder,
/// lists nothing, so nothing is removed.
/// </summary>
internal static class DeployManifest
{
    private const string ModDirPrefix = "modDir=";

    private static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static string ManifestPath(BuildContext context) =>
        Path.Combine(context.ProjectDir, "obj", "ducky-deployed-files.txt");

    /// <summary>
    /// Folder-relative paths that the last successful run recorded for <paramref name="modDir"/>.
    /// Returns an empty set when the manifest is missing, unreadable, or belongs to another folder.
    /// </summary>
    public static HashSet<string> Read(BuildContext context, string modDir)
    {
        var entries = new HashSet<string>(PathComparer);
        var path = ManifestPath(context);

        try
        {
            if (!File.Exists(path))
            {
                return entries;
            }

            var lines = File.ReadAllLines(path);
            if (lines.Length == 0 || !string.Equals(lines[0], ModDirPrefix + FullFolder(modDir), PathComparison))
            {
                return entries;
            }

            foreach (var line in lines.Skip(1).Select(line => line.Trim()).Where(IsSafeRelativePath))
            {
                entries.Add(line);
            }
        }
        catch (Exception ex)
        {
            context.LogWarning($"Could not read the deployed-files manifest, so no file is known to be stale: {ex.Message}");
            entries.Clear();
        }

        return entries;
    }

    /// <summary>
    /// Records what this build plans to write under the mod folder. It runs at the end of a successful post phase.
    /// Never throws: a failed write is logged, and the next reset then removes only what it can account for.
    /// </summary>
    public static void Write(BuildContext context)
    {
        if (string.IsNullOrEmpty(context.ModName))
        {
            return;
        }

        try
        {
            var modDir = ResetDeployLib.GetTargetModDirectory(context);
            WriteEntries(context, modDir, PlannedRelativeFiles(context, modDir));
        }
        catch (Exception ex)
        {
            context.LogWarning(
                $"Could not write the deployed-files manifest; the next reset will not remove files this build no longer writes: {ex.Message}");
        }
    }

    /// <summary>Writes the manifest for <paramref name="modDir"/> with the given folder-relative entries.</summary>
    internal static void WriteEntries(BuildContext context, string modDir, IEnumerable<string> relativePaths)
    {
        var path = ManifestPath(context);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var lines = new List<string> { ModDirPrefix + FullFolder(modDir) };
        lines.AddRange(relativePaths.OrderBy(relative => relative, StringComparer.Ordinal));
        File.WriteAllLines(path, lines);
    }

    /// <summary>
    /// Folder-relative paths of every file the post steps plan to write under <paramref name="modDir"/> in this build.
    /// Each writer lists its own outputs, so an unchanged build plans the same set as the build before it.
    /// </summary>
    public static HashSet<string> PlannedRelativeFiles(BuildContext context, string modDir)
    {
        var planned = new HashSet<string>(PathComparer);
        var outputs = DeployModLib.PlannedOutputs(context)
            .Concat(ILRepackAssembliesLib.PlannedOutputs(context))
            .Concat(CopyDependenciesLib.PlannedOutputs(context))
            .Concat(CopyLocalizationAssetsLib.PlannedOutputs(context))
            .Concat(HotReloadShimStep.PlannedOutputs(context));

        foreach (var file in outputs)
        {
            var relative = RelativeUnder(modDir, file);
            if (relative != null)
            {
                planned.Add(relative);
            }
        }

        return planned;
    }

    /// <summary>Absolute path of a folder-relative manifest entry.</summary>
    public static string ToFullPath(string modDir, string relative) =>
        Path.Combine(modDir, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Forward-slash path of <paramref name="file"/> relative to <paramref name="modDir"/>, or null when it is outside.</summary>
    private static string? RelativeUnder(string modDir, string file)
    {
        var root = FullFolder(modDir) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(file);
        return full.StartsWith(root, PathComparison)
            ? full[root.Length..].Replace(Path.DirectorySeparatorChar, '/')
            : null;
    }

    private static string FullFolder(string folder) =>
        Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>Rejects empty, rooted, or parent-relative entries, so a manifest can never point outside the mod folder.</summary>
    private static bool IsSafeRelativePath(string path) =>
        path.Length > 0 && !Path.IsPathRooted(path) &&
        path.Split('/').All(part => part.Length > 0 && part != "." && part != "..");
}
