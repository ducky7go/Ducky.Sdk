using System.IO.Compression;
using System.Text;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// GeneratePreviewLib writes a 256x256 preview.png: a hash-selected embedded background with the project
/// abbreviation drawn over it. The PNG is checked by reading the signature and IHDR chunk directly, and the
/// pixels by decoding the IDAT stream.
/// </summary>
public class GeneratePreviewLibTests
{
    private const int Size = 256;

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
    public void Execute_CreatesAssetsFolderAndWrites256PixelPngWithValidHeader()
    {
        var (exit, _, _) = LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context()));

        exit.ShouldBe(0);
        var png = File.ReadAllBytes(Path.Combine(_assetsDir, "preview.png"));
        var header = ReadPngHeader(png);
        header.Width.ShouldBe(Size);
        header.Height.ShouldBe(Size);
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

    [Test]
    public void Execute_SameBackgroundButDifferentAbbreviationsProduceDifferentPreviews()
    {
        // Find two names that select the same background but have different abbreviations.
        // "A0mod.Tool" gives "AT", "B1mod.Tool" gives "BT", and so on: the first letter varies, the background index does too.
        var names = Enumerable.Range(0, 500).Select(i => $"{(char)('A' + i % 26)}{i}mod.Tool").ToList();
        var pair = (from a in names
                    from b in names
                    where string.CompareOrdinal(a, b) < 0
                          && BackgroundSelector.IndexFor(a) == BackgroundSelector.IndexFor(b)
                          && ProjectAbbreviation.From(a) != ProjectAbbreviation.From(b)
                    select (a, b)).First();

        var otherAssets = Path.Combine(_root, "Other Mod", "assets");
        LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context(modName: pair.a)));
        LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context(modName: pair.b, assets: otherAssets)));

        File.ReadAllBytes(Path.Combine(_assetsDir, "preview.png"))
            .ShouldNotBe(File.ReadAllBytes(Path.Combine(otherAssets, "preview.png")));
    }

    [Test]
    public void Execute_ArtworkDiffersFromSelectedBackgroundOnlyInsideCentredText()
    {
        const string modName = "Ducky.EntranceMod";
        var index = BackgroundSelector.IndexFor(modName);
        var background = LoadBackground(index);

        var (exit, _, _) = LibTestSupport.Capture(() => GeneratePreviewLib.Execute(Context(modName: modName)));
        exit.ShouldBe(0);
        var rgb = DecodeRgb(File.ReadAllBytes(Path.Combine(_assetsDir, "preview.png")), Size);

        var textRect = GlyphFont.MeasureCentered(Size, Size, ProjectAbbreviation.From(modName));
        var changedInside = 0;
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            var inside = x >= textRect.X && x < textRect.X + textRect.Width && y >= textRect.Y && y < textRect.Y + textRect.Height;
            var actual = Pixel(rgb, x, y, Size);
            var expected = Pixel(background.Pixels, x, y, Size);
            if (inside)
            {
                if (actual != expected) changedInside++;
            }
            else
            {
                actual.ShouldBe(expected, $"pixel ({x},{y}) outside the text must match the background");
            }
        }

        changedInside.ShouldBeGreaterThan(0);
    }

    private static RgbImage LoadBackground(int index)
    {
        var resource = BackgroundSelector.ResourceNameFor(index);
        using var stream = typeof(GeneratePreviewLib).Assembly.GetManifestResourceStream(resource);
        stream.ShouldNotBeNull($"missing embedded resource {resource}");
        return PngDecoder.DecodeRgb(stream);
    }

    /// <summary>Decodes the PNG produced by the preview step into a tightly packed RGB buffer of size x size.</summary>
    private static byte[] DecodeRgb(byte[] png, int size)
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
        var stride = size * 3;
        scanlines.Length.ShouldBe(size * (1 + stride));

        var rgb = new byte[size * stride];
        for (var y = 0; y < size; y++)
        {
            scanlines[y * (1 + stride)].ShouldBe((byte)0, "filter type must be None");
            Array.Copy(scanlines, y * (1 + stride) + 1, rgb, y * stride, stride);
        }

        return rgb;
    }

    private static (byte R, byte G, byte B) Pixel(byte[] rgb, int x, int y, int size)
    {
        var i = (y * size + x) * 3;
        return (rgb[i], rgb[i + 1], rgb[i + 2]);
    }

    [Test]
    public void EncodePng_RoundTripsArbitraryPixels()
    {
        var pixels = new byte[128 * 128 * 3];
        new Random(42).NextBytes(pixels);

        DecodeRgb(GeneratePreviewLib.EncodePng(pixels, 128, 128), 128).ShouldBe(pixels);
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
