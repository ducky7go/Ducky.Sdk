#nullable disable

using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

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

            return result.Success ? 0 : 1;
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
        // Skip if no ModName (library projects)
        if (string.IsNullOrEmpty(context.ModName))
        {
            context.LogInfo("Skipping preview generation: ModName not specified (library project)");
            result.Success = true;
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

        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(size, size);

        // Generate identicon based on SHA256 hash of mod name
        byte[] hash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(modName));
        }

        // Background color from first 3 bytes
        var bgColor = new SixLabors.ImageSharp.PixelFormats.Rgba32(hash[0], hash[1], hash[2]);

        // Foreground color (complementary)
        var fgColor = new SixLabors.ImageSharp.PixelFormats.Rgba32((byte)(255 - hash[0]), (byte)(255 - hash[1]), (byte)(255 - hash[2]));

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
        image.Save(previewPath);
    }

    private static string GetAssetsDirectory(BuildContext context)
    {
        var assetsDir = string.IsNullOrEmpty(context.AssetsDir) ? "assets" : context.AssetsDir;
        return context.GetFullPath(assetsDir);
    }
}

