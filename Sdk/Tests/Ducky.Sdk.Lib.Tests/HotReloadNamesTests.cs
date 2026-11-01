using Ducky.Sdk.ModBehaviours;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Lib.Tests;

/// <summary>
/// Naming of hot reload Core assemblies (ModName.Core.&lt;17-digit UTC stamp&gt;) and the decision that releases a
/// hot reload Core's state on disable.
/// </summary>
public class HotReloadNamesTests
{
    [TestCase("Ducky.Sample.Core.20261031210000123")]
    [TestCase("Ducky.HotReloadSample.Core.20261101042024997")]
    [TestCase("A.Core.00000000000000000")]
    public void CoreNames_Match(string name) => HotReloadNames.IsCoreAssemblyName(name).ShouldBeTrue();

    [TestCase("Ducky.Sample")]
    [TestCase("Ducky.Sample.Core")]
    [TestCase("Ducky.Sample.Core.123")]
    [TestCase("Ducky.Sample.Core.2026103121000012x")]
    [TestCase("Ducky.Sample.CoreX.20261031210000123")]
    [TestCase("Core.20261031210000123")]
    [TestCase("Ducky.Sample.Core.20261031210000123.Extra")]
    [TestCase("")]
    [TestCase(null)]
    public void LookAlikes_DoNotMatch(string? name) => HotReloadNames.IsCoreAssemblyName(name).ShouldBeFalse();

    [Test]
    public void ToModName_StripsTheStampOnlyForCoreNames()
    {
        HotReloadNames.ToModName("Ducky.Sample.Core.20261031210000123").ShouldBe("Ducky.Sample");
        HotReloadNames.ToModName("Ducky.Sample").ShouldBe("Ducky.Sample");
        HotReloadNames.ToModName("Ducky.HotReload.Core").ShouldBe("Ducky.HotReload.Core");
    }

    [Test]
    public void ModBehaviourBase_DevelopmentDetection_UsesTheSameNames()
    {
        ModBehaviourBase.IsDevelopmentAssemblyName("Ducky.Sample.Core.20261031210000123").ShouldBeTrue();
        ModBehaviourBase.IsDevelopmentAssemblyName("Ducky.Sample").ShouldBeFalse();
        ModBehaviourBase.IsDevelopmentAssemblyName("Ducky.Sample.Core").ShouldBeFalse();
    }

    [Test]
    public void ReleaseDecision_OnlyForHotReloadCoreWithNoOtherSubclassEnabled()
    {
        ModBehaviourBase.ShouldReleaseHotReloadState(hotReloadBuild: true, anotherSubclassEnabled: false).ShouldBeTrue();
        ModBehaviourBase.ShouldReleaseHotReloadState(hotReloadBuild: true, anotherSubclassEnabled: true).ShouldBeFalse();
        // Release builds keep the MessageHub host and the buffs running, exactly as before.
        ModBehaviourBase.ShouldReleaseHotReloadState(hotReloadBuild: false, anotherSubclassEnabled: false).ShouldBeFalse();
        ModBehaviourBase.ShouldReleaseHotReloadState(hotReloadBuild: false, anotherSubclassEnabled: true).ShouldBeFalse();
    }
}
