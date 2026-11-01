using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Tests for the preview artwork helpers: background selection, abbreviation rules, the PNG decoder,
/// the glyph overlay, and the ten embedded backgrounds.
/// </summary>
public class PreviewArtworkTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // ---- 1.1 BackgroundSelector ----

    [Test]
    public void BackgroundIndex_IsAlwaysWithinRange()
    {
        for (var i = 0; i < 200; i++)
        {
            var index = BackgroundSelector.IndexFor($"Mod.{i}");
            index.ShouldBeGreaterThanOrEqualTo(0);
            index.ShouldBeLessThan(10);
        }
    }

    [Test]
    public void BackgroundIndex_SameNameGivesSameIndexOnRepeatedCalls()
    {
        var first = BackgroundSelector.IndexFor("Ducky.EntranceMod");
        for (var i = 0; i < 5; i++)
        {
            BackgroundSelector.IndexFor("Ducky.EntranceMod").ShouldBe(first);
        }
    }

    [Test]
    public void BackgroundIndex_TwoHundredNamesCoverAllTenIndexes()
    {
        var seen = Enumerable.Range(0, 200).Select(i => BackgroundSelector.IndexFor($"generated-name-{i}")).ToHashSet();

        seen.Count.ShouldBe(BackgroundSelector.Count);
        seen.ShouldBe(Enumerable.Range(0, 10), ignoreOrder: true);
    }

    // ---- 1.2 ProjectAbbreviation ----

    [TestCase("Ducky.EntranceMod", "DEM")]
    [TestCase("SuperDucky", "SD")]
    [TestCase("Harmony", "H")]
    [TestCase("HTTPServer", "HS")]
    [TestCase("Alpha.Beta.Gamma.Delta", "ABG")]
    [TestCase("my-cool_mod name", "MCM")]
    [TestCase("2Fast.Mod", "2FM")]
    [TestCase("ducky.entranceMod", "DEM")]
    public void Abbreviation_FollowsWordRules(string name, string expected)
    {
        ProjectAbbreviation.From(name).ShouldBe(expected);
    }

    [TestCase("模组")]
    [TestCase("鸭科夫模组.测试")]
    public void Abbreviation_NameWithoutAsciiLettersIsThreeLettersAndStable(string name)
    {
        var first = ProjectAbbreviation.From(name);

        first.Length.ShouldBe(3);
        first.ShouldAllBe(c => c >= 'A' && c <= 'Z');
        ProjectAbbreviation.From(name).ShouldBe(first);
        ProjectAbbreviation.From(name).ShouldBe(first);
    }

    // ---- 2.1 PngDecoder ----

    [Test]
    public void Decoder_RoundTripsEncodePngOutput()
    {
        var pixels = new byte[128 * 128 * 3];
        new Random(42).NextBytes(pixels);

        var image = PngDecoder.DecodeRgb(GeneratePreviewLib.EncodePng(pixels, 128, 128));

        image.Width.ShouldBe(128);
        image.Height.ShouldBe(128);
        image.Pixels.ShouldBe(pixels);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Decoder_UndoesEachScanlineFilter(int filter)
    {
        const int width = 9, height = 7;
        var pixels = new byte[width * height * 3];
        new Random(7).NextBytes(pixels);

        var png = BuildPng(width, height, colourType: 2, pixels, bytesPerPixel: 3, filterFor: _ => (byte)filter);
        var image = PngDecoder.DecodeRgb(png);

        image.Pixels.ShouldBe(pixels);
    }

    [Test]
    public void Decoder_DropsAlphaFromRgbaInput()
    {
        const int width = 6, height = 4;
        var rgb = new byte[width * height * 3];
        var rgba = new byte[width * height * 4];
        var random = new Random(11);
        for (var p = 0; p < width * height; p++)
        {
            random.NextBytes(rgba.AsSpan(p * 4, 4));
            rgb[p * 3] = rgba[p * 4];
            rgb[p * 3 + 1] = rgba[p * 4 + 1];
            rgb[p * 3 + 2] = rgba[p * 4 + 2];
        }

        var image = PngDecoder.DecodeRgb(BuildPng(width, height, colourType: 6, rgba, bytesPerPixel: 4, filterFor: y => (byte)(y % 5)));

        image.Pixels.ShouldBe(rgb);
    }

    [Test]
    public void Decoder_IgnoresAncillaryChunks()
    {
        var pixels = new byte[4 * 4 * 3];
        new Random(3).NextBytes(pixels);
        var png = BuildPng(4, 4, colourType: 2, pixels, bytesPerPixel: 3, filterFor: _ => 0,
            extraChunkBeforeIdat: ("tEXt", Encoding.Latin1.GetBytes("Software\0ComfyUI")));

        PngDecoder.DecodeRgb(png).Pixels.ShouldBe(pixels);
    }

    [Test]
    public void Decoder_RejectsInterlacedImages()
    {
        var png = BuildPng(4, 4, colourType: 2, new byte[4 * 4 * 3], bytesPerPixel: 3, filterFor: _ => 0, interlace: 1);

        Should.Throw<NotSupportedException>(() => PngDecoder.DecodeRgb(png)).Message.ShouldContain("interlaced");
    }

    [Test]
    public void Decoder_RejectsSixteenBitImages()
    {
        var png = BuildPng(4, 4, colourType: 2, new byte[4 * 4 * 3], bytesPerPixel: 3, filterFor: _ => 0, bitDepth: 16);

        Should.Throw<NotSupportedException>(() => PngDecoder.DecodeRgb(png)).Message.ShouldContain("bit depth");
    }

    [Test]
    public void Decoder_RejectsNonPngInput()
    {
        Should.Throw<InvalidDataException>(() => PngDecoder.DecodeRgb(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
    }

    // ---- 2.2 GlyphFont ----

    [Test]
    public void GlyphFont_DrawsOnlyInsideCentredTextRectangle()
    {
        const int size = 256;
        var buffer = Enumerable.Repeat((byte)128, size * size * 3).ToArray();
        var before = (byte[])buffer.Clone();

        var rect = GlyphFont.DrawCentered(buffer, size, size, "DEM");

        rect.X.ShouldBeGreaterThanOrEqualTo(0);
        rect.Y.ShouldBeGreaterThanOrEqualTo(0);
        (rect.X + rect.Width).ShouldBeLessThanOrEqualTo(size);
        (rect.Y + rect.Height).ShouldBeLessThanOrEqualTo(size);

        // Centred: the glyph block (the rectangle without its one-cell shadow) has its middle within a pixel of the image middle.
        var shadow = GlyphFont.ScaleFor(size, size, 3);
        var blockWidth = rect.Width - shadow;
        var blockHeight = rect.Height - shadow;
        Math.Abs((rect.X + blockWidth / 2) - size / 2).ShouldBeLessThanOrEqualTo(1);
        Math.Abs((rect.Y + blockHeight / 2) - size / 2).ShouldBeLessThanOrEqualTo(1);

        var changed = 0;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var i = (y * size + x) * 3;
            if (buffer.AsSpan(i, 3).SequenceEqual(before.AsSpan(i, 3))) continue;

            changed++;
            var inside = x >= rect.X && x < rect.X + rect.Width && y >= rect.Y && y < rect.Y + rect.Height;
            inside.ShouldBeTrue($"pixel ({x},{y}) changed outside the text rectangle");
        }

        changed.ShouldBeGreaterThan(0);
    }

    [Test]
    public void GlyphFont_DrawingIsIdenticalOnRepeatedRuns()
    {
        byte[] Draw()
        {
            var buffer = Enumerable.Repeat((byte)90, 256 * 256 * 3).ToArray();
            GlyphFont.DrawCentered(buffer, 256, 256, "DEM");
            return buffer;
        }

        Draw().ShouldBe(Draw());
    }

    [Test]
    public void GlyphFont_SingleLetterStaysInsideImage()
    {
        var buffer = new byte[256 * 256 * 3];

        var rect = GlyphFont.DrawCentered(buffer, 256, 256, "H");

        (rect.Y + rect.Height).ShouldBeLessThanOrEqualTo(256);
        (rect.X + rect.Width).ShouldBeLessThanOrEqualTo(256);
    }

    // ---- 4.1 / 4.2 embedded backgrounds ----

    [Test]
    public void EmbeddedBackgrounds_AreExactlyTenNamedResources()
    {
        var names = typeof(GeneratePreviewLib).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("PreviewBackgrounds/", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        names.ShouldBe(Enumerable.Range(0, 10).Select(BackgroundSelector.ResourceNameFor).ToArray());
    }

    [Test]
    public void EmbeddedBackgrounds_DecodeAt256AndAreDistinct()
    {
        var rawBytes = new List<byte[]>();
        for (var index = 0; index < BackgroundSelector.Count; index++)
        {
            var resource = BackgroundSelector.ResourceNameFor(index);
            using var stream = typeof(GeneratePreviewLib).Assembly.GetManifestResourceStream(resource);
            stream.ShouldNotBeNull($"missing embedded resource {resource}");

            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            var png = copy.ToArray();
            rawBytes.Add(png);

            var image = PngDecoder.DecodeRgb(png);
            image.Width.ShouldBe(256, resource);
            image.Height.ShouldBe(256, resource);
        }

        for (var a = 0; a < rawBytes.Count; a++)
        for (var b = a + 1; b < rawBytes.Count; b++)
        {
            rawBytes[a].SequenceEqual(rawBytes[b]).ShouldBeFalse($"backgrounds {a} and {b} are byte-identical");
        }
    }

    // ---- test PNG encoder (independent of EncodePng: supports every scanline filter and extra chunks) ----

    private static byte[] BuildPng(int width, int height, int colourType, byte[] pixels, int bytesPerPixel,
        Func<int, byte> filterFor, int bitDepth = 8, int interlace = 0, (string Type, byte[] Data)? extraChunkBeforeIdat = null)
    {
        var stride = width * bytesPerPixel;
        var raw = new MemoryStream();
        var prior = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var line = pixels.AsSpan(y * stride, stride).ToArray();
            var filter = filterFor(y);
            raw.WriteByte(filter);
            raw.Write(FilterLine(filter, line, prior, bytesPerPixel));
            prior = line;
        }

        var idat = new MemoryStream();
        using (var z = new ZLibStream(idat, CompressionLevel.Optimal, leaveOpen: true))
        {
            z.Write(raw.ToArray());
        }

        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4), height);
        ihdr[8] = (byte)bitDepth;
        ihdr[9] = (byte)colourType;
        ihdr[12] = (byte)interlace;

        var png = new MemoryStream();
        png.Write(PngSignature);
        WriteChunk(png, "IHDR", ihdr);
        if (extraChunkBeforeIdat is { } extra) WriteChunk(png, extra.Type, extra.Data);
        WriteChunk(png, "IDAT", idat.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static byte[] FilterLine(byte filter, byte[] line, byte[] prior, int bpp)
    {
        var output = new byte[line.Length];
        for (var i = 0; i < line.Length; i++)
        {
            var a = i >= bpp ? line[i - bpp] : (byte)0;
            var b = prior[i];
            var c = i >= bpp ? prior[i - bpp] : (byte)0;
            byte predictor = filter switch
            {
                0 => 0,
                1 => a,
                2 => b,
                3 => (byte)((a + b) / 2),
                4 => TestPaeth(a, b, c),
                _ => throw new ArgumentOutOfRangeException(nameof(filter)),
            };
            output[i] = (byte)(line[i] - predictor);
        }

        return output;
    }

    private static byte TestPaeth(byte a, byte b, byte c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        return pb <= pc ? b : c;
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        var typeBytes = Encoding.ASCII.GetBytes(type);
        var length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, TestCrc32(typeBytes.Concat(data).ToArray()));
        stream.Write(crc);
    }

    private static uint TestCrc32(byte[] bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }

        return ~crc;
    }
}
