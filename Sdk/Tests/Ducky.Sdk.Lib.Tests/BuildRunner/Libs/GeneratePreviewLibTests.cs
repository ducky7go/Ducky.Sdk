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

        exit.ShouldBe(BuildScriptGlobals.SkipExitCode);
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
}
