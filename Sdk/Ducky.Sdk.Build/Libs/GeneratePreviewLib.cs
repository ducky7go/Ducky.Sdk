#nullable disable

using System;
using System.IO;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Linq;
using System.Text;

/// <summary>
/// Script library for enhanced preview generation with better error handling and logging
/// Entry point: GeneratePreviewLib.Execute(BuildContext context)
/// </summary>
public class GeneratePreviewLib
{
    public class GenerationResult
    {
        public bool Success { get; set; }
        public string PreviewPath { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public int ExitCode { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Main entry point for generating preview images
    /// </summary>
    /// <param name="context">BuildContext containing configuration</param>
    /// <returns>Exit code (0 = success, 1 = failure)</returns>
    public static int Execute(BuildContext context)
    {
        try
        {
            context.LogInfo("=== Generate Preview Results ===");

            var result = Generate(context);

            context.LogInfo($"Success: {result.Success}");
            context.LogInfo($"Preview Path: {result.PreviewPath}");
            context.LogInfo($"Exit Code: {result.ExitCode}");
            context.LogInfo($"Generated At: {result.GeneratedAt:u}");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                context.LogError($"Error: {result.ErrorMessage}");
            }

            return result.ExitCode;
        }
        catch (Exception ex)
        {
            context.LogError($"Generate preview exception: {ex.Message}");
            return 1;
        }
    }

    public static GenerationResult Generate(BuildContext context)
    {
        var result = new GenerationResult();

        try
        {
            context.LogInfo("Starting preview generation");

            // Check if this should be skipped
            if (ShouldSkipGeneration(context, result))
            {
                return result;
            }

            var assetsDir = GetAssetsDirectory(context);
            var previewPath = Path.Combine(assetsDir ?? "", "preview.png");

            // Check if preview already exists
            if (File.Exists(previewPath))
            {
                context.LogInfo($"Preview already exists: {previewPath}");
                result.Success = true;
                result.PreviewPath = previewPath;
                return result;
            }

            // Ensure assets directory exists
            Directory.CreateDirectory(assetsDir!);

            // Generate preview directly
            try
            {
                GeneratePreviewImage(context, previewPath);
                result.Success = true;
                result.PreviewPath = previewPath;
                context.LogInfo($"Preview generated successfully: {previewPath}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Preview generation failed: {ex.Message}";
                context.LogError($"Exception: {ex}");
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Preview generation error: {ex.Message}";
            context.LogError($"Exception: {ex}");
        }

        return result;
    }

    private static bool ShouldSkipGeneration(BuildContext context, GenerationResult result)
    {
        // Skip if no ModName (library projects) or IsModLib (library projects)
        if (string.IsNullOrEmpty(context.ModName) || context.IsModLib)
        {
            context.LogInfo(
                $"Skipping preview generation: {(string.IsNullOrEmpty(context.ModName) ? "ModName not specified" : "IsModLib is true")} (library project)");
            result.Success = true;
            result.ExitCode = SkipExitCode;
            return true;
        }

        return false;
    }

    private static void GeneratePreviewImage(BuildContext context, string previewPath)
    {
        const int size = 128;
        const int gridSize = 5; // 5x5 grid
        const int cellSize = size / gridSize;

        var modName = string.IsNullOrEmpty(context.ModName) ? Path.GetFileName(context.ProjectDir) : context.ModName;

        // Generate identicon based on SHA256 hash of mod name
        var hash = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(modName));

        // Background color from first 3 bytes, foreground is its complement
        var bg = new[] { hash[0], hash[1], hash[2] };
        var fg = new[] { (byte)(255 - hash[0]), (byte)(255 - hash[1]), (byte)(255 - hash[2]) };

        // Plain RGB pixel buffer (no image library needed for flat-colour cells)
        var rgb = new byte[size * size * 3];
        for (var i = 0; i < size * size; i++)
        {
            rgb[i * 3] = bg[0];
            rgb[i * 3 + 1] = bg[1];
            rgb[i * 3 + 2] = bg[2];
        }

        void FillCell(int cx, int cy)
        {
            for (var y = cy * cellSize; y < (cy + 1) * cellSize; y++)
            for (var x = cx * cellSize; x < (cx + 1) * cellSize; x++)
            {
                var i = (y * size + x) * 3;
                rgb[i] = fg[0];
                rgb[i + 1] = fg[1];
                rgb[i + 2] = fg[2];
            }
        }

        // Symmetric pattern (mirrored horizontally): hash bytes decide which cells to fill
        for (var y = 0; y < gridSize; y++)
        {
            for (var x = 0; x < (gridSize + 1) / 2; x++) // Only half, mirror the rest
            {
                if (hash[(y * 3 + x) % hash.Length] % 2 != 0) continue;

                FillCell(x, y);
                if (x != gridSize / 2) // Don't double-draw center column
                {
                    FillCell(gridSize - 1 - x, y);
                }
            }
        }

        File.WriteAllBytes(previewPath, EncodePng(rgb, size, size));
    }

    /// <summary>Encodes a tightly packed 8-bit RGB buffer as a PNG (no filtering, zlib-compressed).</summary>
    internal static byte[] EncodePng(byte[] rgb, int width, int height)
    {
        using var idat = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionLevel.Optimal, leaveOpen: true))
        {
            for (var y = 0; y < height; y++)
            {
                z.WriteByte(0); // filter type 0: None
                z.Write(rgb, y * width * 3, width * 3);
            }
        }

        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 2; // colour type 2: RGB

        using var png = new MemoryStream();
        png.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        WritePngChunk(png, "IHDR", ihdr);
        WritePngChunk(png, "IDAT", idat.ToArray());
        WritePngChunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void WritePngChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        var buf = new byte[4];

        BinaryPrimitives.WriteInt32BigEndian(buf, data.Length);
        stream.Write(buf);
        stream.Write(typeBytes);
        stream.Write(data);

        BinaryPrimitives.WriteUInt32BigEndian(buf, Crc32(Crc32(0, typeBytes), data));
        stream.Write(buf);
    }

    private static readonly uint[] CrcTable = Enumerable.Range(0, 256).Select(n =>
    {
        var c = (uint)n;
        for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
        return c;
    }).ToArray();

    private static uint Crc32(uint crc, byte[] data)
    {
        crc ^= 0xFFFFFFFFu;
        foreach (var b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }

    private static string GetAssetsDirectory(BuildContext context)
    {
        var assetsDir = string.IsNullOrEmpty(context.AssetsDir) ? "assets" : context.AssetsDir;
        return context.GetFullPath(assetsDir);
    }
}
