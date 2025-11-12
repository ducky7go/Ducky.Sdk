#!/usr/bin/env dotnet-script
// generate-preview.csx
// Usage:
//   dotnet script generate-preview.csx <AssetsDirectory>
// Behavior:
//   If <AssetsDirectory>/info.ini exists and contains "name = <ModName>"
//   and <AssetsDirectory>/preview.png does NOT exist,
//   generate a 256x256 identicon-style image based on hash of ModName.
// Exit Codes:
//   0 = success (including no-op if preview.png already exists)
//   1 = usage error / missing required files

#nullable disable
#r "nuget: SixLabors.ImageSharp, 3.1.6"
#r "nuget: SixLabors.ImageSharp.Drawing, 2.1.4"

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

int Fail(string msg, int code = 1)
{
    Console.Error.WriteLine($"[generate-preview][ERROR] {msg}");
    return code;
}

if (Args.Count < 1)
    return Fail("Usage: dotnet script generate-preview.csx <AssetsDirectory>");

var assetsDir = Args[0];
var infoPath = Path.Combine(assetsDir, "info.ini");
var previewPath = Path.Combine(assetsDir, "preview.png");

Console.WriteLine($"[generate-preview] Assets directory: {assetsDir}");
Console.WriteLine($"[generate-preview] Info.ini: {infoPath}");
Console.WriteLine($"[generate-preview] Preview.png: {previewPath}");

if (!Directory.Exists(assetsDir))
    return Fail($"Assets directory not found: {assetsDir}");

if (File.Exists(previewPath))
{
    Console.WriteLine("[generate-preview] preview.png already exists. Skipping generation.");
    return 0;
}

if (!File.Exists(infoPath))
{
    Console.WriteLine("[generate-preview] info.ini not found. Cannot determine mod name.");
    return 0;
}

// Parse mod name from info.ini
var infoLines = File.ReadAllLines(infoPath);
var nameRegex = new Regex(@"^\s*name\s*=\s*(.+?)\s*$", RegexOptions.IgnoreCase);
string modName = infoLines
    .Select(l => nameRegex.Match(l))
    .Where(m => m.Success)
    .Select(m => m.Groups[1].Value)
    .FirstOrDefault();

if (string.IsNullOrWhiteSpace(modName))
{
    Console.WriteLine("[generate-preview] No 'name' field found in info.ini. Skipping.");
    return 0;
}

Console.WriteLine($"[generate-preview] Mod name: {modName}");
Console.WriteLine($"[generate-preview] Generating identicon for preview.png...");

// Generate identicon based on SHA256 hash of mod name
byte[] hash;
using (var sha = SHA256.Create())
{
    hash = sha.ComputeHash(Encoding.UTF8.GetBytes(modName));
}

// Create 256x256 image
const int size = 256;
const int gridSize = 5; // 5x5 grid
const int cellSize = size / gridSize;

using (var image = new Image<Rgba32>(size, size))
{
    // Background color from first 3 bytes
    var bgColor = new Rgba32(hash[0], hash[1], hash[2]);
    
    // Foreground color (complementary)
    var fgColor = new Rgba32((byte)(255 - hash[0]), (byte)(255 - hash[1]), (byte)(255 - hash[2]));

    image.Mutate(ctx =>
    {
        // Fill background
        ctx.BackgroundColor(bgColor);
        
        // Generate symmetric pattern (mirrored horizontally)
        // Use hash bytes to determine which cells to fill
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < (gridSize + 1) / 2; x++) // Only half, mirror the rest
            {
                int byteIndex = (y * 3 + x) % hash.Length;
                bool fill = (hash[byteIndex] % 2) == 0;

                if (fill)
                {
                    // Fill left side
                    ctx.Fill(fgColor, new RectangleF(x * cellSize, y * cellSize, cellSize, cellSize));
                    // Mirror to right side
                    if (x != gridSize / 2) // Don't double-draw center column
                    {
                        ctx.Fill(fgColor, new RectangleF((gridSize - 1 - x) * cellSize, y * cellSize, cellSize, cellSize));
                    }
                }
            }
        }
    });

    // Save as PNG
    image.SaveAsPng(previewPath);
}

Console.WriteLine($"[generate-preview] Successfully generated preview.png");
return 0;
