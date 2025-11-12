#!/usr/bin/env dotnet-script
// ensure-info-ini.csx
// Usage:
//   dotnet script ensure-info-ini.csx <AssetsDirectory> <ModName>
// Behavior:
//   If <AssetsDirectory>/info.ini does NOT exist,
//   generate a basic info.ini with name, displayName, and description
//   based on the provided ModName.
// Exit Codes:
//   0 = success (including no-op if info.ini already exists)
//   1 = usage error

#nullable disable

using System;
using System.IO;
using System.Text;

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
    Console.WriteLine("[ensure-info-ini] info.ini already exists. Skipping generation.");
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
