using System;
using System.IO;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// The mod folder choice (<see cref="Helper.ResolveModFolderPath"/>) and the failure rule of <see cref="ModFolder"/>,
/// checked with plain string inputs. No game is loaded.
/// </summary>
public class ModFolderTests
{
    private const string CoreA = "Ducky.Sample.Core.20261031210000123";
    private const string CoreB = "Ducky.Sample.Core.20261101042024997";

    private static readonly string ShimFolder = Path.Combine("mods", "Ducky.Sample");
    private static readonly string ShimLocation = Path.Combine(ShimFolder, "Ducky.Sample.dll");

    private static (Func<string?> Name, Func<string?> Location)[] ShimLoaded() =>
        new (Func<string?>, Func<string?>)[] { (() => "Ducky.Sample", () => ShimLocation) };

    private static (Func<string?> Name, Func<string?> Location)[] NothingLoaded() =>
        Array.Empty<(Func<string?> Name, Func<string?> Location)>();

    [Test]
    public void NormalLoad_UsesTheAssemblysOwnFolder()
    {
        var own = Path.Combine(ShimFolder, "Ducky.Sample.dll");

        Helper.ResolveModFolderPath("Ducky.Sample", () => own, NothingLoaded()).ShouldBe(ShimFolder);
    }

    [Test]
    public void CoreWithoutLocation_UsesTheLoadedShimOfTheSameMod()
    {
        Helper.ResolveModFolderPath(CoreA, () => string.Empty, ShimLoaded()).ShouldBe(ShimFolder);
    }

    [Test]
    public void SameFolder_AcrossReloads()
    {
        var before = Helper.ResolveModFolderPath(CoreA, () => string.Empty, ShimLoaded());
        var after = Helper.ResolveModFolderPath(CoreB, () => string.Empty, ShimLoaded());

        after.ShouldBe(before);
        before.ShouldBe(ShimFolder);
    }

    [Test]
    public void CoreWithoutLocation_AndNoShim_IsUnknown()
    {
        Helper.ResolveModFolderPath(CoreA, () => string.Empty, NothingLoaded()).ShouldBeNull();
    }

    [Test]
    public void Require_ReturnsAKnownFolderUnchanged()
    {
        ModFolder.Require(ShimFolder).ShouldBe(ShimFolder);
    }

    [TestCase(null)]
    [TestCase("")]
    public void Require_NoFolder_ThrowsAnErrorNamingBothSources(string? folder)
    {
        var error = Should.Throw<InvalidOperationException>(() => ModFolder.Require(folder));

        error.Message.ShouldContain("assembly");
        error.Message.ShouldContain("hot reload shim");
    }

    [Test]
    public void Require_NeverReturnsAGuessedPath()
    {
        var folder = Helper.ResolveModFolderPath(CoreA, () => string.Empty, NothingLoaded());

        Should.Throw<InvalidOperationException>(() => ModFolder.Require(folder));
    }
}
