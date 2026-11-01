#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Up-to-date stamps for build steps that copy files.
/// A step computes a fingerprint of its inputs before it runs and writes it to obj/ducky-stamps/&lt;step&gt;.stamp
/// only after a successful run. The next run may skip the step only when the fingerprint still matches and every
/// expected output exists. Any stamp failure is logged and treated as "not up to date": it never fails the build.
/// </summary>
internal static class BuildStamp
{
    /// <summary>
    /// Fingerprint of a step: the runner's assembly version, the caller's key and, for each existing input file,
    /// its full path, length and UTC last-write ticks, in ordinal path order.
    /// Returns null when the fingerprint cannot be computed, so the step runs.
    /// </summary>
    public static string? Compute(IEnumerable<string> inputFiles, string key)
    {
        try
        {
            var builder = new StringBuilder();
            builder.Append("sdk=").Append(typeof(BuildStamp).Assembly.GetName().Version).Append('\n');
            builder.Append("key=").Append(key).Append('\n');

            foreach (var file in inputFiles.Select(Path.GetFullPath).Distinct().OrderBy(p => p, StringComparer.Ordinal))
            {
                if (!File.Exists(file))
                {
                    continue;
                }

                var info = new FileInfo(file);
                builder.Append(file).Append('|')
                    .Append(info.Length).Append('|')
                    .Append(info.LastWriteTimeUtc.Ticks).Append('\n');
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// True when the stored stamp equals <paramref name="fingerprint"/> and every expected output file or folder exists.
    /// </summary>
    public static bool IsUpToDate(BuildContext context, string step, string? fingerprint, IEnumerable<string> expectedOutputs)
    {
        try
        {
            if (fingerprint == null)
            {
                return false;
            }

            var path = StampPath(context, step);
            if (!File.Exists(path) || File.ReadAllText(path).Trim() != fingerprint)
            {
                return false;
            }

            return expectedOutputs.All(output => File.Exists(output) || Directory.Exists(output));
        }
        catch (Exception ex)
        {
            context.LogWarning($"Up-to-date check for {step} failed, the step will run: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stores the fingerprint of a successful run. Does nothing when the fingerprint is null.
    /// </summary>
    public static void Write(BuildContext context, string step, string? fingerprint)
    {
        if (fingerprint == null)
        {
            return;
        }

        try
        {
            var path = StampPath(context, step);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, fingerprint);
        }
        catch (Exception ex)
        {
            context.LogWarning($"Could not write the up-to-date stamp for {step}; the next build will run it again: {ex.Message}");
        }
    }

    /// <summary>
    /// The context values that change what a step does. Steps append their own destinations to this key.
    /// </summary>
    public static string ContextKey(BuildContext context) => string.Join("|",
        $"ModName={context.ModName}",
        $"DeployMod={context.DeployMod}",
        $"IsModLib={context.IsModLib}",
        $"ModsDirectory={context.ModsDirectory}",
        $"ManagedDirectory={context.ManagedDirectory}",
        $"LocalizationAssetsDir={context.LocalizationAssetsDir}",
        $"AssetsDir={context.AssetsDir}",
        $"Configuration={context.Configuration}",
        $"TargetFramework={context.TargetFramework}");

    private static string StampPath(BuildContext context, string step) =>
        Path.Combine(context.ProjectDir, "obj", "ducky-stamps", $"{step}.stamp");
}
