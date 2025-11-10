#!/usr/bin/env dotnet-script
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// Usage: dotnet script extract-lkeys-json.csx <ProjectObjDir> <OutputJsonPath>
// 从生成的 LKeys.*.metadata.g.cs 文件中提取 JSON 内容
// 这个脚本读取 Source Generator 生成的 .g.cs 源文件，提取其中的 JSON 字符串常量

if (Args == null || Args.Count < 2)
{
    Console.Error.WriteLine("Usage: extract-lkeys-json.csx <ProjectObjDir> <OutputJsonPath>");
    Console.Error.WriteLine("  <ProjectObjDir>: obj/Debug/netXX directory containing generated .g.cs files");
    Console.Error.WriteLine("  <OutputJsonPath>: Path to write extracted JSON file");
    return 1;
}

var objDir = Args[0];
var outputPath = Args[1];

if (!Directory.Exists(objDir))
{
    Console.Error.WriteLine($"Error: Object directory not found: {objDir}");
    return 1;
}

Console.WriteLine($"Searching for metadata files in: {objDir}");

// 搜索所有 LKeys.*.metadata.g.cs 文件
// Source Generator 生成的文件通常在以下位置之一：
// 1. obj/Debug/netXX/generated/<GeneratorName>/<GeneratorAssembly>/*.g.cs
// 2. obj/Debug/netXX/*.g.cs
var metadataFiles = Directory.GetFiles(objDir, "LKeys.*.metadata.g.cs", SearchOption.AllDirectories);

Console.WriteLine($"Found {metadataFiles.Length} metadata file(s)");

if (metadataFiles.Length == 0)
{
    Console.WriteLine("No LKeys metadata files found. This project may not have localization keys.");
    Console.WriteLine("Skipping JSON extraction and CSV generation.");
    // 返回特殊退出码 (2) 表示没有本地化键，但这不是错误
    return 2;
}

// 取第一个找到的文件（通常只有一个）
var metadataFile = metadataFiles[0];
Console.WriteLine($"Extracting JSON from: {metadataFile}");

try
{
    var content = File.ReadAllText(metadataFile);
    
    // 使用正则表达式提取 JsonData 常量的值
    // 格式: internal const string JsonData = @"...";
    // 注意：在逐字字符串中，" 被转义为 ""，所以需要匹配 "" 或非 " 字符
    var pattern = @"internal\s+const\s+string\s+JsonData\s*=\s*@""((?:[^""]|"""")*)""";
    var match = Regex.Match(content, pattern, RegexOptions.Singleline);
    
    if (!match.Success)
    {
        Console.Error.WriteLine("Error: Could not find JsonData constant in metadata file");
        Console.Error.WriteLine("File content preview:");
        Console.Error.WriteLine(content.Substring(0, Math.Min(500, content.Length)));
        return 1;
    }
    
    // 提取 JSON 字符串（需要将 "" 转回 "）
    var escapedJson = match.Groups[1].Value;
    var jsonContent = escapedJson.Replace("\"\"", "\"");
    
    // 验证 JSON 格式
    try
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var keyCount = doc.RootElement.GetProperty("keyCount").GetInt32();
        Console.WriteLine($"Successfully extracted JSON with {keyCount} keys");
    }
    catch (JsonException ex)
    {
        Console.Error.WriteLine($"Warning: Extracted content is not valid JSON: {ex.Message}");
        Console.Error.WriteLine("Content preview:");
        Console.Error.WriteLine(jsonContent.Substring(0, Math.Min(200, jsonContent.Length)));
        Console.Error.WriteLine("Writing anyway...");
    }
    
    // 写入输出文件
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
    File.WriteAllText(outputPath, jsonContent, new UTF8Encoding(false));
    
    Console.WriteLine($"JSON written to: {outputPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
    return 1;
}
