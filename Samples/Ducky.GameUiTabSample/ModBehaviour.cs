using Ducky.Sdk.GameUi;

namespace Ducky.GameUiTabSample;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        // The SDK keeps the panel and its tab in the game's Tab bar and removes them when the mod is disabled.
        Subscriptions.Add(GameUiTabs.Register<PlayerAttributesView>(new GameUiTabOptions
        {
            // Same icon as the map tab, tinted so it can be told apart.
            Tint = new Color(1f, 0.8f, 0.3f),
        }));
    }

    protected override void ModDisabled()
    {
        // Nothing to undo: Subscriptions disposes the registration, which removes the panel and its tab.
    }
}
