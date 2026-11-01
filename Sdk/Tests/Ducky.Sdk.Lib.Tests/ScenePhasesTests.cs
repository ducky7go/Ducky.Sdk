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

    [Test]
    public void IsMainMenu_TrueOnlyForTheMainMenuScene()
    {
        ScenePhases.IsMainMenu(ScenePhases.MainMenuSceneName).ShouldBeTrue();
        ScenePhases.IsMainMenu("MainMenu").ShouldBeTrue();
    }

    [TestCase("")]
    [TestCase("mainmenu")]
    [TestCase("MainMenu2")]
    [TestCase("Base_Main")]
    [TestCase("Level_01")]
    [TestCase(" MainMenu")]
    public void IsMainMenu_FalseForOtherNames(string name) => ScenePhases.IsMainMenu(name).ShouldBeFalse();

    [Test]
    public void IsMainMenu_FalseForNull() => ScenePhases.IsMainMenu(null).ShouldBeFalse();

    [Test]
    public void BaseAndLevelPhases_AreUnchangedByTheMainMenuCheck()
    {
        ScenePhases.Of(ScenePhases.MainMenuSceneName).ShouldBe(ScenePhase.Other);
        ScenePhases.Of("Base_Main").ShouldBe(ScenePhase.Base);
        ScenePhases.Of("Level_01").ShouldBe(ScenePhase.Raid);
    }
}
