using System;
using System.Collections.Generic;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// <see cref="ModInfo"/> and the folder resolution behind it. The resolution is tested through its pure seam, so no
/// real assembly location or Unity object is needed.
/// </summary>
public class ModInfoTests
{
    private const string RealDll = "/mods/Ducky/Ducky.Sample.dll";

    private static (Func<string?> Name, Func<string?> Location) Loaded(string? name, string? location) =>
        (() => name, () => location);

    private static (Func<string?> Name, Func<string?> Location) Dynamic(string name) =>
        (() => name, () => throw new NotSupportedException("Dynamic assemblies have no location."));

    [Test]
    public void Folder_FromLocatedAssembly_IsItsDirectory()
    {
        var folder = Helper.ResolveModFolderPath("Ducky.Sample", () => RealDll, []);

        folder.ShouldBe("/mods/Ducky");
    }

    [Test]
    public void Folder_HotReloadCore_FallsBackToTheLoadedShim()
    {
        var folder = Helper.ResolveModFolderPath(
            "Ducky.Sample.Core.20261031210000123",
            () => null,
            [Loaded("Ducky.Sample", RealDll)]);

        folder.ShouldBe("/mods/Ducky");
    }

    [Test]
    public void Folder_DynamicAssemblyIsSkipped_AndTheShimStillFound()
    {
        var folder = Helper.ResolveModFolderPath(
            "Ducky.Sample.Core.20261031210000123",
            () => null,
            [Dynamic("Ducky.Sample.Core.20261031210000124"), Loaded("Ducky.Sample", RealDll)]);

        folder.ShouldBe("/mods/Ducky");
    }

    [Test]
    public void Folder_ShimWithoutLocation_IsNull_AndDoesNotThrow()
    {
        var folder = Helper.ResolveModFolderPath(
            "Ducky.Sample.Core.20261031210000123",
            () => null,
            [Loaded("Ducky.Sample", null)]);

        folder.ShouldBeNull();
    }

    [Test]
    public void Folder_NoAssembly_IsNull()
    {
        var folder = Helper.ResolveModFolderPath("Ducky.Sample.Core.20261031210000123", () => null, []);

        folder.ShouldBeNull();
    }

    [Test]
    public void Folder_LocationThatThrowsOtherThanNotSupported_IsSkipped()
    {
        var failing = (Name: (Func<string?>)(() => "Broken"), Location: (Func<string?>)(() => throw new InvalidOperationException()));
        var folder = Helper.ResolveModFolderPath(
            "Ducky.Sample.Core.20261031210000123",
            () => null,
            [failing, Loaded("Ducky.Sample", RealDll)]);

        folder.ShouldBe("/mods/Ducky");
    }

    [Test]
    public void Folder_LocationWithoutDirectory_IsNull()
    {
        Helper.ResolveModFolderPath("Ducky.Sample", () => "Ducky.Sample.dll", []).ShouldBeNull();
    }

    [Test]
    public void AssetsPath_FollowsFolder()
    {
        ModInfo.AssetsPathFor("/mods/MyMod").ShouldBe(System.IO.Path.Combine("/mods/MyMod", "assets"));
    }

    [Test]
    public void AssetsPath_NullWhenFolderUnknown()
    {
        ModInfo.AssetsPathFor(null).ShouldBeNull();
        ModInfo.AssetsPathFor(string.Empty).ShouldBeNull();
    }

    [TestCase("12345", "steam.12345")]
    [TestCase("Ducky.Sample", "local.Ducky.Sample")]
    [TestCase("123abc", "local.123abc")]
    public void Id_PrefixFollowsTheFolderName(string folder, string expected) =>
        Helper.ResolveModId(folder, "Ducky Sample").ShouldBe(expected);

    [Test]
    public void Id_FallsBackToTheNameWhenNoFolder()
    {
        Helper.ResolveModId(null, "Ducky Sample").ShouldBe("Ducky Sample");
        Helper.ResolveModId(string.Empty, "Ducky Sample").ShouldBe("Ducky Sample");
    }

    [TestCase("Ducky.Sample.Core.20261031210000123", true)]
    [TestCase("Ducky.Sample", false)]
    public void IsHotReloadCore_DependsOnTheAssemblyNamePattern(string assemblyName, bool expected) =>
        HotReloadNames.IsCoreAssemblyName(assemblyName).ShouldBe(expected);

    [Test]
    public void IsHotReloadCore_ForThisTestAssembly_IsFalse()
    {
        // The test assembly is a release-style name, so the computed flag is false.
        ModInfo.IsHotReloadCore.ShouldBeFalse();
    }
}
