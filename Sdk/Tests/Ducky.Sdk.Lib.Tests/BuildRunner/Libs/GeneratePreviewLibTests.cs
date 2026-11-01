using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Task 5.3: GeneratePreviewLib writes a 128x128 identicon preview.png. The PNG is checked by reading the
/// signature and IHDR chunk directly, without decoding the image.
/// </summary>
public class GeneratePreviewLibTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private string _root = null!;
    private string _projectDir = null!;
    private string _assetsDir = null!;

    [SetUp]
    public void SetUp()
    {
        _root = LibTestSupport.NewTempRoot("preview");
        _projectDir = Path.Combine(_root, "Ducky Mod");
        _assetsDir = Path.Combine(_projectDir, "assets");
        Directory.CreateDirectory(_projectDir);
    }

    [TearDown]
    public void TearDown() => LibTestSupport.DeleteTempRoot(_root);

    private BuildContext Context(string? modName = "Ducky.EntranceMod", bool isModLib = false, string? assets = null) =>
        LibTestSupport.NewContext(_projectDir, modName: modName, assetsDir: assets ?? _assetsDir, isModLib: isModLib);

    private static (int Width, int Height, byte BitDepth, byte ColorType) ReadPngHeader(byte[] png)
    {
        png.Take(8).ToArray().ShouldBe(PngSignature);
        // The first chunk must be IHDR: 4-byte length (13), 4-byte type, then width, height, bit depth, colour type.
        var length = ReadBigEndian(png, 8);
        Encoding.ASCII.GetString(png, 12, 4).ShouldBe("IHDR");
        length.ShouldBe(13);
        return (ReadBigEndian(png, 16), ReadBigEndian(png, 20), png[24], png[25]);
    }

    private static int ReadBigEndian(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void Execute_SkipsWhenModNameIsMissingOrProjectIsLibrary(bool modNameMissing, bool isModLib)
    {
        var context = Context(modName: modNameMissing ? null : "Ducky.EntranceMod", isModLib: isModLib);

        var (exit, _, _) = LibTestSupport.Capture(() => GeneratePreviewLib.Execute(context));

        exit.ShouldBe(RunnerExitCodes.SkipExitCode);
        File.Exists(Path.Combine(_assetsDir, "preview.png")).ShouldBeFalse();
    }

    [Test]
    public void Execute_CreatesAssetsFolderAndWrites128PixelPngWithValidHeader()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context()));

        exit.ShouldBe(0);
        var png = File.ReadAllBytes(Path.Combine(_assetsDir, "preview.png"));
        var header = ReadPngHeader(png);
        header.Width.ShouldBe(128);
        header.Height.ShouldBe(128);
        header.BitDepth.ShouldBe((byte)8);
        // Legal PNG colour types: 0 grey, 2 RGB, 3 palette, 4 grey+alpha, 6 RGBA.
        new byte[] { 0, 2, 3, 4, 6 }.ShouldContain(header.ColorType);
    }

    [Test]
    public void Execute_LeavesExistingPreviewUntouched()
    {
        LibTestSupport.WriteText(Path.Combine(_assetsDir, "preview.png"), "existing-preview");

        var (exit, output, _) = LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context()));

        exit.ShouldBe(0);
        output.ShouldContain("Preview already exists");
        File.ReadAllText(Path.Combine(_assetsDir, "preview.png")).ShouldBe("existing-preview");
    }

    [Test]
    public void Execute_SameModNameProducesIdenticalPreviewBytes()
    {
        var otherAssets = Path.Combine(_root, "Other Mod", "assets");
        LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context()));
        LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context(assets: otherAssets)));

        File.ReadAllBytes(Path.Combine(_assetsDir, "preview.png"))
            .ShouldBe(File.ReadAllBytes(Path.Combine(otherAssets, "preview.png")));
    }

    [Test]
    public void Execute_DifferentModNamesProduceDifferentPreviews()
    {
        var otherAssets = Path.Combine(_root, "Other Mod", "assets");
        LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context(modName: "Ducky.EntranceMod")));
        LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context(modName: "Ducky.TryHarmony", assets: otherAssets)));

        File.ReadAllBytes(Path.Combine(_assetsDir, "preview.png"))
            .ShouldNotBe(File.ReadAllBytes(Path.Combine(otherAssets, "preview.png")));
    }

    /// <summary>Decodes the PNG produced by the preview step into a tightly packed 128x128 RGB buffer.</summary>
    private static byte[] DecodeRgb(byte[] png)
    {
        var idat = new MemoryStream();
        var offset = 8;
        var lastType = "";
        while (offset < png.Length)
        {
            var length = ReadBigEndian(png, offset);
            lastType = Encoding.ASCII.GetString(png, offset + 4, 4);
            if (lastType == "IDAT") idat.Write(png, offset + 8, length);
            offset += 12 + length;
        }

        lastType.ShouldBe("IEND");
        png.TakeLast(4).ToArray().ShouldBe(new byte[] { 0xAE, 0x42, 0x60, 0x82 }); // CRC32 of "IEND"

        idat.Position = 0;
        using var raw = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionMode.Decompress)) z.CopyTo(raw);
        var scanlines = raw.ToArray();
        scanlines.Length.ShouldBe(128 * (1 + 128 * 3));

        var rgb = new byte[128 * 128 * 3];
        for (var y = 0; y < 128; y++)
        {
            scanlines[y * (1 + 128 * 3)].ShouldBe((byte)0, "filter type must be None");
            Array.Copy(scanlines, y * (1 + 128 * 3) + 1, rgb, y * 128 * 3, 128 * 3);
        }

        return rgb;
    }

    private static (byte R, byte G, byte B) Pixel(byte[] rgb, int x, int y)
    {
        var i = (y * 128 + x) * 3;
        return (rgb[i], rgb[i + 1], rgb[i + 2]);
    }

    private byte[] GenerateRgb(string modName)
    {
        var assets = Path.Combine(_root, modName, "assets");
        LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context(modName: modName, assets: assets)));
        return DecodeRgb(File.ReadAllBytes(Path.Combine(assets, "preview.png")));
    }

    [TestCase("Ducky.EntranceMod")]
    [TestCase("Ducky.TryHarmony")]
    [TestCase("a")]
    public void Execute_RendersIdenticonMatchingHashDerivedGrid(string modName)
    {
        const int cell = 128 / 5; // 25px; the 5x5 grid covers 125px, the last 3px are background margin
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(modName));
        var bg = (R: hash[0], G: hash[1], B: hash[2]);
        var fg = (R: (byte)(255 - hash[0]), G: (byte)(255 - hash[1]), B: (byte)(255 - hash[2]));

        var rgb = GenerateRgb(modName);

        var filledCells = 0;
        for (var gy = 0; gy < 5; gy++)
        {
            for (var gx = 0; gx < 5; gx++)
            {
                // Mirrored layout: columns 3 and 4 copy columns 1 and 0.
                var sourceX = gx < 3 ? gx : 4 - gx;
                var expected = hash[(gy * 3 + sourceX) % hash.Length] % 2 == 0 ? fg : bg;
                if (expected == fg) filledCells++;

                // Every pixel of the cell must carry the expected colour, not just its corner.
                for (var y = gy * cell; y < (gy + 1) * cell; y++)
                for (var x = gx * cell; x < (gx + 1) * cell; x++)
                    Pixel(rgb, x, y).ShouldBe(expected, $"cell ({gx},{gy}) pixel ({x},{y})");
            }
        }

        // Margin right of / below the grid is plain background.
        for (var i = 0; i < 128; i++)
        {
            for (var m = 125; m < 128; m++)
            {
                Pixel(rgb, m, i).ShouldBe(bg);
                Pixel(rgb, i, m).ShouldBe(bg);
            }
        }

        // Guard against a degenerate all-background / all-foreground picture only when the hash itself isn't.
        var expectedFilled = Enumerable.Range(0, 5).Sum(gy =>
            Enumerable.Range(0, 5).Count(gx => hash[(gy * 3 + (gx < 3 ? gx : 4 - gx)) % hash.Length] % 2 == 0));
        filledCells.ShouldBe(expectedFilled);
    }

    [Test]
    public void Execute_ProducesOnlyTwoComplementaryColoursAndHorizontalSymmetry()
    {
        var rgb = GenerateRgb("Ducky.EntranceMod");

        var colours = new HashSet<(byte, byte, byte)>();
        for (var y = 0; y < 128; y++)
        for (var x = 0; x < 128; x++)
            colours.Add(Pixel(rgb, x, y));

        colours.Count.ShouldBe(2);
        var (a, b) = (colours.First(), colours.Last());
        ((byte)(255 - a.Item1), (byte)(255 - a.Item2), (byte)(255 - a.Item3)).ShouldBe(b);

        // Grid area (125px wide) is mirrored left-to-right.
        for (var y = 0; y < 125; y++)
        for (var x = 0; x < 125; x++)
            Pixel(rgb, x, y).ShouldBe(Pixel(rgb, 124 - x, y), $"pixel ({x},{y}) must mirror ({124 - x},{y})");
    }

    [Test]
    public void EncodePng_RoundTripsArbitraryPixels()
    {
        var pixels = new byte[128 * 128 * 3];
        new Random(42).NextBytes(pixels);

        DecodeRgb(GeneratePreviewLib.EncodePng(pixels, 128, 128)).ShouldBe(pixels);
    }

    [Test]
    public void EncodePng_WritesCorrectChunkCrcs()
    {
        var png = GeneratePreviewLib.EncodePng(new byte[128 * 128 * 3], 128, 128);

        // Independent CRC32 (bitwise, no table) over each chunk's type + data.
        static uint Crc(byte[] buf, int start, int len)
        {
            var crc = 0xFFFFFFFFu;
            for (var i = start; i < start + len; i++)
            {
                crc ^= buf[i];
                for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }

            return ~crc;
        }

        var offset = 8;
        var chunks = 0;
        while (offset < png.Length)
        {
            var length = ReadBigEndian(png, offset);
            var stored = (uint)ReadBigEndian(png, offset + 8 + length);
            Crc(png, offset + 4, 4 + length).ShouldBe(stored, $"chunk #{chunks} CRC");
            offset += 12 + length;
            chunks++;
        }

        offset.ShouldBe(png.Length);
        chunks.ShouldBeGreaterThanOrEqualTo(3); // IHDR, IDAT..., IEND
    }
}
