using System.IO;
using System.Runtime.CompilerServices;

namespace Ducky.Sdk.Lib.Tests.Runner;

/// <summary>
/// Task 6.5: the sample mods build with the default validation severity without errors. The validation step runs on a
/// copy of each sample's assets (it writes missing.csv into the assets folder), so the repository Samples are not edited.
/// </summary>
public class SamplesLocalesValidationTests
{
    private static string SamplesDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives in Sdk/Tests/Ducky.Sdk.Lib.Tests/BuildRunner/Libs; the Samples folder is at the Ducky.Sdk root.
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..", "Samples"));
    }

    [Test]
    public void SampleAssets_ValidateAtTheDefaultSeverityWithoutErrors()
    {
        var samples = SamplesDirectory();
        var assetFolders = Directory.GetDirectories(samples, "assets", SearchOption.AllDirectories)
            .Where(folder => Directory.Exists(Path.Combine(folder, "Locales")))
            .Where(folder => !folder.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                             && !folder.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();
        assetFolders.ShouldNotBeEmpty();

        var root = LibTestSupport.NewTempRoot("samples-validate");
        try
        {
            foreach (var source in assetFolders)
            {
                var copy = Path.Combine(root, Path.GetFileName(Path.GetDirectoryName(source)!));
                CopyDirectory(source, copy);
                var context = LibTestSupport.NewContext(copy, modName: "Sample", assetsDir: copy);

                var (exit, _, error) = LibTestSupport.Capture(() => ValidateLocalizationLib.Execute(context));

                exit.ShouldBe(0, customMessage: source);
                error.ShouldNotContain("error:", customMessage: source);
            }
        }
        finally
        {
            LibTestSupport.DeleteTempRoot(root);
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }

        foreach (var folder in Directory.GetDirectories(source))
        {
            CopyDirectory(folder, Path.Combine(target, Path.GetFileName(folder)));
        }
    }
}
