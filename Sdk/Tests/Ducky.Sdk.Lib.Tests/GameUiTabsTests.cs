using Ducky.Sdk.GameUi;

namespace Ducky.Sdk.Lib.Tests;

[TestFixture]
public class GameUiTabsTests
{
    private static readonly string[] Tabs = { "BackpackButton", "PlayerStatsButton", "QuestsButton", "MapButton" };

    [Test]
    public void No_insert_after_puts_the_tab_last()
    {
        GameUiTabs.ResolveInsertIndex(Tabs, null).ShouldBe(4);
    }

    [Test]
    public void Insert_after_a_known_tab_puts_it_right_behind()
    {
        GameUiTabs.ResolveInsertIndex(Tabs, "PlayerStatsButton").ShouldBe(2);
    }

    [Test]
    public void An_unknown_tab_falls_back_to_last()
    {
        GameUiTabs.ResolveInsertIndex(Tabs, "NoSuchButton").ShouldBe(4);
    }
}
