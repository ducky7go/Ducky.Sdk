using Ducky.Sdk.GameEvents;

namespace Ducky.Sdk.Lib.Tests;

public class ScenePhasesTests
{
    [TestCase("Base_Main", ScenePhase.Base)]
    [TestCase("BaseHome", ScenePhase.Base)]
    [TestCase("Level_01", ScenePhase.Raid)]
    [TestCase("LevelX", ScenePhase.Raid)]
    [TestCase("MainMenu", ScenePhase.Other)]
    [TestCase("base_x", ScenePhase.Other)]
    [TestCase("BASE_x", ScenePhase.Other)]
    [TestCase("level_x", ScenePhase.Other)]
    [TestCase("", ScenePhase.Other)]
    public void Of_ClassifiesByOrdinalPrefix(string name, ScenePhase expected) =>
        ScenePhases.Of(name).ShouldBe(expected);

    [Test]
    public void Of_Null_IsOther() => ScenePhases.Of(null).ShouldBe(ScenePhase.Other);
}
