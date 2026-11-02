# Ducky.GameUiSample: real-game verification notes

Run on the Windows host (Windows 11, display 2880×1800 at 200%, so Windows reports 1440×900 logical) with
Escape from Duckov, test save slot 6 (base scene `Base_SceneV2`), Newbe.DebugMod and Ducky.GameUiSample both
loaded (`Mod Loaded:` lines in `Player.log`, no exception lines naming either mod).

## 5.2 Canvas scale (check a)

| Object | Field | Value |
| --- | --- | --- |
| Game HUD text `BulletCount` (under `HUDCanvas`) | `m_CanvasScaleFactor` | 1.125 (fontSize 18) |
| Sample text `Body` (under the sample canvas) | `m_CanvasScaleFactor` | 1.125 |

The sample canvas uses `GameUiScaler.Apply`, which copies the `HUDCanvas` scaler (2560×1440, match 0). Both values equal 1.125 = 2880 / 2560.

## 5.3 Comparison and states (partly verified)

- Screenshot `ui-base-4.png` shows the panel in the base: text in the game font, ladder sizes, rounded panel with gold rim, and the three button looks (selected gold, grey normal, disabled dark).
- Uppercase letter height: compared by eye only. Not measured in pixels.
- Hovered look: NOT captured. The OS pointer is hidden by the game in this mode, so the hover highlight was never shown. Accepted as not verified (decision of 2026-11-02). A simulated hover through `Selectable.DoStateTransition` was not possible: DebugMod resolves the sample type to a stale hot-reload Core (see docs/game-ui.md, Known limitations).
- Raised shadow: the rounded sibling is drawn behind the panel but is barely visible on the dark background; not confirmed by eye.

## 5.4 Hot reload with the panel open

Six hot reloads with the panel open. Each unload reported `childObjects=0` (the old panel was removed) and each load reported `childObjects=1` (one new panel). `Player.log` lines matching `Exception` or `NullReference`: 0.

## 5.5 Fallback (partly verified)

- `ForceFallback` on: the panel is a square `Image` with a square gold `Outline` rim, and all text and buttons are visible (`ui-fallback.png`). The fill looks brownish because the 96%-opaque fill lets the outline colour show through. Accepted for now; noted as a visual difference from the rounded look.
- `ForceFallback` off again: the rounded look returns (`ui-restored.png`).
- NOT verified: "exactly one fallback warning is logged". In this game install the SDK logger is a no-op: `Player.log` shows `[ModBehaviourBase] Failed to get a valid logger. Using NoOpLogger.` for every mod, so the warning and the sample's `Log.Info` are dropped. Cause (confirmed at runtime): `Type.GetType("Serilog.Log, Ducky.Logging.DeveloperEdition")` returns null because the Serilog code is in the stamped Core, not the shim. Accepted as not verified; logging left as is (decision of 2026-11-02).

## 7.2 Built output (inspected with System.Reflection.Metadata)

- Debug hot-reload output (`Publish/Ducky.GameUiSample/`): no `Plugins.dll`. Core assembly: references `ProceduralImage` and `UniformModifier`, has an assembly reference to `Plugins`, defines no `ProceduralImage` or `UniformModifier` type; defines `ProceduralImageBridge`, so the rounded path is compiled in.
- Release (merged with ILRepack), `Ducky.GameUiSample.dll`: references `ProceduralImage` and `UniformModifier`, defines neither; no `Plugins.dll` in the published folder.

Screenshots: `ui-base-1.png` (before the mod panel was visible: it had been destroyed by the scene load),
`ui-base-2.png` (after the `DontDestroyOnLoad` fix), `ui-base-3.png` and `ui-base-4.png` (layout fix), `ui-fallback.png`, `ui-restored.png`, `ui-hover2.png`. Kept in the session scratchpad, not in the repo.
