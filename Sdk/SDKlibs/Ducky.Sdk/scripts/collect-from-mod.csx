#!/usr/bin/env dotnet-script
// collect-from-mod.csx
// Usage:
//   dotnet script collect-from-mod.csx <ModOutputDirectory> <AssetsDirectory>
// Behavior:
//   If <ModOutputDirectory>/info.ini contains a line: publishedFileId = <digits>
//   and <AssetsDirectory>/info.ini does NOT already contain any publishedFileId line,
//   append that line to the end of assets/info.ini (preserving newline).
//   If assets already has a publishedFileId (any value) we skip.
// Exit Codes:
//   0 = success (including no-op conditions)
//   1 = usage error / missing required files

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

int Fail(string msg, int code=1)
{
	Console.Error.WriteLine($"[collect-from-mod][ERROR] {msg}");
	return code;
}

// Robust Args resolution: find args after the .csx script path
List<string> GetScriptArgs()
{
    var all = Environment.GetCommandLineArgs();
    var idx = Array.FindIndex(all, s => s.EndsWith(".csx", StringComparison.OrdinalIgnoreCase));
    if (idx >= 0 && idx + 1 < all.Length)
    {
        return all.Skip(idx + 1).ToList();
    }
    // Fallback: best effort (may include host exe at [0])
    return all.Skip(1).ToList();
}
var Args = GetScriptArgs();
if (Args == null || Args.Count < 2)
	return Fail("Usage: dotnet script collect-from-mod.csx <ModOutputDirectory> <AssetsDirectory>");

var modDir = Args[0];
var assetsDir = Args[1];
var modInfoPath = Path.Combine(modDir, "info.ini");
var assetsInfoPath = Path.Combine(assetsDir, "info.ini");

Console.WriteLine($"[collect-from-mod] Mod directory: {modDir}");
Console.WriteLine($"[collect-from-mod] Assets directory: {assetsDir}");
Console.WriteLine($"[collect-from-mod] Mod info.ini: {modInfoPath}");
Console.WriteLine($"[collect-from-mod] Assets info.ini: {assetsInfoPath}");

if (!Directory.Exists(modDir))
	return Fail($"Mod directory not found: {modDir}");
if (!Directory.Exists(assetsDir))
	return Fail($"Assets directory not found: {assetsDir}");
if (!File.Exists(modInfoPath))
{
	Console.WriteLine("[collect-from-mod] Mod info.ini not found. Nothing to sync.");
	return 0;
}
if (!File.Exists(assetsInfoPath))
{
	Console.WriteLine("[collect-from-mod] Assets info.ini not found. Creating new file.");
	File.WriteAllText(assetsInfoPath, "", Encoding.UTF8);
}

var modLines = File.ReadAllLines(modInfoPath);
var assetsLines = File.ReadAllLines(assetsInfoPath);

var publishedRegex = new Regex("^\\s*publishedFileId\\s*=\\s*(\\d+)\\s*$", RegexOptions.IgnoreCase);
string modPublishedLine = modLines.FirstOrDefault(l => publishedRegex.IsMatch(l));
bool assetsHasPublished = assetsLines.Any(l => publishedRegex.IsMatch(l));

if (assetsHasPublished)
{
	Console.WriteLine("[collect-from-mod] Assets already contain publishedFileId. Skipping sync.");
	return 0;
}

if (string.IsNullOrWhiteSpace(modPublishedLine))
{
	Console.WriteLine("[collect-from-mod] Mod info.ini has no publishedFileId line. Nothing to sync.");
	return 0;
}

// Append publishedFileId to assets info.ini
Console.WriteLine($"[collect-from-mod] Syncing publishedFileId from mod -> assets: {modPublishedLine.Trim()}");
var builder = new StringBuilder();
builder.Append(string.Join(Environment.NewLine, assetsLines));
if (assetsLines.Length > 0 && !assetsLines.Last().EndsWith("\n") && !assetsLines.Last().EndsWith("\r"))
{
	// Ensure newline before appending if file not empty and doesn't end with newline
	builder.AppendLine();
}
else if (assetsLines.Length == 0)
{
	// Start with nothing -> no extra blank line needed
}
builder.AppendLine(modPublishedLine.Trim());
File.WriteAllText(assetsInfoPath, builder.ToString(), Encoding.UTF8);
Console.WriteLine("[collect-from-mod] Append completed successfully.");
return 0;
