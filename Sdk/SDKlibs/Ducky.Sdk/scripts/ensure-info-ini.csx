#!/usr/bin/env dotnet-script
// ensure-info-ini.csx
// Usage:
//   dotnet script ensure-info-ini.csx <AssetsDirectory> <ModName>
// Behavior:
//   If <AssetsDirectory>/info.ini does NOT exist,
//   generate a basic info.ini with name, displayName, and description
//   based on the provided ModName.
//   If <AssetsDirectory>/info.ini exists, validate and sync the name field
//   with ModName to ensure consistency.
// Exit Codes:
//   0 = success (including no-op if info.ini already exists and name matches)
//   1 = usage error

#nullable disable

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

int Fail(string msg, int code = 1)
{
    Console.Error.WriteLine($"[ensure-info-ini][ERROR] {msg}");
    return code;
}

if (Args.Count < 2)
    return Fail("Usage: dotnet script ensure-info-ini.csx <AssetsDirectory> <ModName>");

var assetsDir = Args[0];
var modName = Args[1];

Console.WriteLine($"[ensure-info-ini] Assets directory: {assetsDir}");
Console.WriteLine($"[ensure-info-ini] Mod name: {modName}");

if (!Directory.Exists(assetsDir))
{
    Console.WriteLine($"[ensure-info-ini] Assets directory does not exist, creating: {assetsDir}");
    Directory.CreateDirectory(assetsDir);
}

var infoPath = Path.Combine(assetsDir, "info.ini");

if (File.Exists(infoPath))
{
    Console.WriteLine("[ensure-info-ini] info.ini already exists. Checking name synchronization...");

    // Read existing INI content
    var existingLines = File.ReadAllLines(infoPath);
    var nameRegex = new Regex(@"^\s*name\s*=\s*(.+?)\s*$", RegexOptions.IgnoreCase);
    var nameLine = existingLines.FirstOrDefault(l => nameRegex.IsMatch(l));

    if (nameLine != null)
    {
        var match = nameRegex.Match(nameLine);
        var existingName = match.Groups[1].Value.Trim();

        if (existingName != modName)
        {
            Console.WriteLine($"[ensure-info-ini] Name mismatch found. INI name: '{existingName}', ModName: '{modName}'");
            Console.WriteLine("[ensure-info-ini] Updating name field to match ModName...");

            // Replace the name line
            var updatedLines = existingLines.Select(line =>
                nameRegex.IsMatch(line) ? $"name = {modName}" : line
            ).ToArray();

            File.WriteAllText(infoPath, string.Join(Environment.NewLine, updatedLines), Encoding.UTF8);
            Console.WriteLine($"[ensure-info-ini] Successfully updated name field to: {modName}");
        }
        else
        {
            Console.WriteLine("[ensure-info-ini] Name field already matches ModName. No changes needed.");
        }
    }
    else
    {
        Console.WriteLine("[ensure-info-ini] No name field found in existing info.ini. Adding name field...");
        var updatedContent = string.Join(Environment.NewLine, existingLines) +
                            $"{Environment.NewLine}name = {modName}{Environment.NewLine}";
        File.WriteAllText(infoPath, updatedContent, Encoding.UTF8);
        Console.WriteLine($"[ensure-info-ini] Successfully added name field: {modName}");
    }

    return 0;
}

Console.WriteLine("[ensure-info-ini] info.ini not found. Generating basic info.ini...");

// Generate basic info.ini content
var iniContent = new StringBuilder();
iniContent.AppendLine($"name = {modName}");
iniContent.AppendLine($"displayName = {modName}");
iniContent.AppendLine($"description = A mod for Escape from Duckov");

// Write to file
File.WriteAllText(infoPath, iniContent.ToString(), Encoding.UTF8);

Console.WriteLine($"[ensure-info-ini] Successfully generated info.ini with basic metadata");
Console.WriteLine($"[ensure-info-ini]   name = {modName}");
Console.WriteLine($"[ensure-info-ini]   displayName = {modName}");
Console.WriteLine($"[ensure-info-ini]   description = A mod for Escape from Duckov");

return 0;
