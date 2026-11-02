using Ducky.Sdk.GameUi;

namespace Ducky.GameUiSample;

public class ModBehaviour : ModBehaviourBase
{
    // Task 5.5: set this to true, hot reload, and check the ordinary-graphic fallback in the real game. Set it back to false afterwards.
    private const bool ForceFallbackCheck = false;

    private GameObject? _root;

    protected override void ModEnabled()
    {
        GameUiRounded.ForceFallback = ForceFallbackCheck;
        _root = GameUiSamplePanel.Build();
        Log.Info("Ducky.GameUiSample panel built (ForceFallback={ForceFallback})", GameUiRounded.ForceFallback);
    }

    protected override void ModDisabled()
    {
        if (_root != null)
        {
            UnityEngine.Object.Destroy(_root);
        }

        _root = null;
    }
}
