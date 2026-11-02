# Game UI for mods

How to build mod UI that looks like *Escape from Duckov*, what the SDK provides for it (`Ducky.Sdk.GameUi`), what was observed in the game, and what is still not verified.

Facts are marked **Observed** (read from the running game or from the game's own code) or **Not verified**. Measurements come from the real game, Escape from Duckov, German language, test save slot 6, display 2880×1800 at 200% scaling (Windows reports 1440×900 logical), on 2026-11-01.

## Status

| Part | Status |
| --- | --- |
| Scale from the game's canvas (`GameUiScaler.Apply`) | Verified in the game: the mod text's `m_CanvasScaleFactor` is 1.125, the same as the game HUD text. |
| Type ladder and radius tiers (`GameUiMetrics`) | Values verified against the game's text sizes (see Observed). Radius tiers are conventions. |
| Font lookup (`GameUiFont.Get`) | Verified in the game: the sample text uses the game's template font. |
| Rounded fill (`GameUiRounded.AddRounded`) | Verified in the game: rounded panel and button. |
| Rim (`GameUiRounded.AddRim`) | Verified in the game: gold border on the rounded panel. |
| Raised shadow (sibling behind the panel) | Built, but barely visible on the dark game background. Not confirmed by eye. |
| Fallback (`ForceFallback`, missing `Plugins.dll`) | Verified in the game: square `Image` with a square `Outline` rim, content not covered, rounded look restored when the switch is off. **Not verified:** that exactly one fallback warning is logged (see Known limitations). |
| Hovered button look | **Not verified.** The OS pointer is hidden in the game's aim mode, so no hover was captured. |
| Uppercase letter height comparison | Judged by eye on one screenshot, not measured in pixels. |

## 1. Observed in the game

### The game's UI is uGUI + TextMeshPro + a procedural rounded-rect image

| What | Count in the base scene | Note |
| --- | --- | --- |
| `Canvas` | 12 | `HUDCanvas`, `GameplayUICanvas`, `PauseMenu`, `SceneLoader`, `DialogueBubbleCanvas`, ... |
| `Duckov.UI.View` subclasses | 34 | `InventoryView`, `CraftView`, `StockShopView`, `ItemCustomizeSelectionView`, `PerkTreeView`, `QuestView`, ... |
| `Button` / `ScrollRect` / `Toggle` / `Slider` / `TMP_InputField` | 469 / 151 / 21 / 19 / 13 | all stock `UnityEngine.UI` |
| `TextMeshProUGUI` | 1840 | |
| `Image` | 13705 | many are `ProceduralImage` (name `Background`) |
| `Sprite` | 1326 (1310 distinct names) | only sprites loaded at that moment; not every asset on disk |
| `TMP_FontAsset` | 7 | `ResourceHanRoundedCN-Medium SDF`, `Jua-Regular SDF`, `Number_EN`, `PressStart2P-Regular SDF`, `7segment SDF`, `fusion-pixel-12px-monospaced SDF`, `UnifontExMono SDF` |

- **Panels and slots are not sprites.** The dark translucent rounded panels, slot frames and buttons are `UnityEngine.UI.ProceduralImage.ProceduralImage` (third-party UI Procedural Image, compiled into the game). Its corner shape comes from a modifier component: `UniformModifier` (one `Radius`; an `Alert` instance reads `22.5`) or `RoundModifier`. `BorderWidth` and `FalloffDistance` sit on the image. The type is defined in `Managed/Plugins.dll`; `Managed/TeamSoda.Duckov.Core.dll` only contains the name `ProceduralImageHashProvider`, a different type.
- **Views are a framework, not loose panels.** A `View` has a `FadeGroup` (`Duckov.UI.Animations`) whose elements are `RectTransformFade` / `CanvasGroupFade` children (`Title`, `Content`). `ItemCustomizeSelectionView` reuses `SlotDisplay(Clone)` prefab instances under a `GridLayout`. Views are looked up through `GameplayUIManager.GetViewInstance<T>()`.
- **Few real sprites are used for chrome.** Names that look like building blocks: `Rounded Rect`, `Rounded Rect small`, `Panel`, `UIMask`, `Frame_Basic_Rectangle`, `Rect16`, `Circle`, `MetalSlice`, `ToggleBox_On`, `ToggleBox_Off`, `Slider01`, `checkmark`, `arrow_back_24dp_FFFFFF_...` (the back arrow in the top-left of the workbench), `leftButton` / `middleButton` / `rightButton`, `HorizontalFrameMask_16_Left/Right`, `UpFrameMask16`, `DownFrameMask16`. Item icons (`Backpack_LV1`, `Card_*`, buff icons) are content, not chrome.
- **The font comes from the game.** `GameplayDataSettings.UIStyle.TemplateTextUGUI.font` is the TMP font the game's text uses (`ResourceHanRoundedCN-Medium SDF`).
- **The canvas scaler.** The HUD text `BulletCount` (under `HUDCanvas`) reports `m_CanvasScaleFactor` 1.125 and `fontSize` 18 at 2880×1800. The scale is the screen width divided by the reference width 2560.
- **Ready-made entry points that work from a mod.**
  - `NotificationText.Push(string)` (static): observed in a screenshot, a black pill at the top centre (`Hello from probe`).
  - `Duckov.UI.DialogueBubbles.DialogueBubblesManager.Show(string text, Transform target, float yOffset, bool needInteraction, bool skippable, float speed, float duration)` (static, returns `UniTask`): signature read, not called.
  - Opening a native view: `ItemCustomizeSelectionView.Show()` produced the workbench screen (left panel "Deine Gegenstände", right empty detail card). Other kinds are in `Newbe.DebugMod/Features/ViewOpener.cs`.
- **How other mods reuse the game's UI.** `DuckMarket` (decompiled under `Newbe.Dod/decompiles/DuckMarket.Core/`) subclasses `View` and clones existing objects: `Object.Instantiate(ItemOperationMenu)` for its menu, `Instantiate(Button)` for extra buttons, `Instantiate(template, parent)` for list rows.

### What this means for building "similar" UI

Ranked by effort, cheapest first:

1. **Call the game's own widget** (`NotificationText.Push`, dialogue bubble, open a native `View`). No layout code.
2. **Clone a live game prefab instance** (`Instantiate` a `Button`, `SlotDisplay`, list-row template found under an existing view) and re-parent it. The look is exact; the risk is coupling to child names and to the view being loaded.
3. **Build from `new GameObject` with the game's pieces**: rounded fill and rim (`GameUiRounded`), the game TMP font (`GameUiFont`), the ladder sizes (`GameUiMetrics`), a `FadeGroup` for show and hide if you want the game's animation, and the few chrome sprites above. This is the route for a view the game does not already have, and it needs no sprite files in the mod. The sample `Samples/Ducky.GameUiSample` is built this way.
4. Hand-drawn `Image` with your own sprites. Works, but looks foreign.

## 2. Usage

### 2.1 Scale the canvas when the UI opens

```csharp
var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
GameUiScaler.Apply(scaler);   // copies HUDCanvas / GameplayUICanvas, falls back to 2560×1440 match 0
```

Call it each time the mod's UI opens, so a changed game scaler is picked up. Do not call it every frame. Nothing is cached and no event is subscribed.

### 2.2 Type ladder

Use `GameUiMetrics.Display` (36), `Headline` (28), `Title` (24), `Body` (20), `Caption` (18) and `Micro` (16) for text. Nothing goes below 16: the game's smallest text is 16. Do not scale a ladder size down to fit: 18 × 75% = 13.5 is the thing to avoid, because it is below the smallest size the game uses and would look out of place next to the game's text.

### 2.3 Font

```csharp
var font = GameUiFont.Get();          // game UI font, or the TextMesh Pro default font
if (font != null) label.font = font;
```

One lookup for all callers. It returns the game font when it can be resolved, the TMP default otherwise, and writes one warning in the fallback case.

### 2.4 Rounded panel, rim and raised shadow

```csharp
var fill = GameUiRounded.AddRounded(panelObject, GameUiMetrics.RadiusPanel);   // Image?, set colour on it
var rim  = GameUiRounded.AddRim(panelObject, GameUiMetrics.RadiusPanel, 2f);   // Image?, null in the fallback
```

- `AddRounded` makes a solid rounded rectangle. Calling it again on the same object updates the radius. If the object has another kind of `Graphic`, that component is removed.
- `AddRim` adds a border-only child named `Rim` that follows the corners, is excluded from layout and does not block clicks. In the fallback it adds a square `Outline` to the host instead and returns null.
- **Raised shadow:** create a rounded sibling object *before* the panel, offset down by a few units and dark, so it is drawn behind the panel. The SDK does not wrap this.
- **Why not `Outline` or `Shadow` for the shadow:** square `Outline` and `Shadow` effects follow the rectangle of the graphic, not the rounded corners, so on a rounded panel they show square corners. They are used only in the fallback for that reason.

### 2.5 Minimal panel example

This is the core of `Samples/Ducky.GameUiSample/GameUiSamplePanel.cs`, reduced to one panel with a title:

```csharp
using Ducky.Sdk.GameUi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class MinimalPanel
{
    internal static GameObject Build()
    {
        var root = new GameObject("MyPanel", typeof(RectTransform));
        UnityEngine.Object.DontDestroyOnLoad(root);   // the mod enables on the title screen; keep it across scene loads
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        GameUiScaler.Apply(root.AddComponent<CanvasScaler>());
        root.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 580f);

        var fill = GameUiRounded.AddRounded(panel, GameUiMetrics.RadiusPanel);
        if (fill != null) fill.color = new Color(0.12f, 0.12f, 0.14f, 0.96f);
        var rim = GameUiRounded.AddRim(panel, GameUiMetrics.RadiusPanel, 2f);
        if (rim != null) rim.color = new Color(0.85f, 0.70f, 0.30f, 1f);

        var title = new GameObject("Title", typeof(RectTransform));
        title.transform.SetParent(panel.transform, false);
        var titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(860f, GameUiMetrics.Headline * 1.6f);
        titleRect.anchoredPosition = new Vector2(0f, 250f);
        var text = title.AddComponent<TextMeshProUGUI>();
        var font = GameUiFont.Get();
        if (font != null) text.font = font;
        text.text = "Panel title";
        text.fontSize = GameUiMetrics.Headline;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return root;
    }
}
```

Destroying the returned root (in `ModDisabled`, for example) removes the whole panel.

### 2.6 When to set `IncludeGameUi`

Set `<IncludeGameUi>true</IncludeGameUi>` in the mod's csproj when you want the rounded look. The SDK compiles the rounded bridge into each mod from its source, so the rounded path exists only if the mod references the game's `Plugins.dll`, and that reference is added by this property. A mod without it compiles and runs, but always takes the ordinary-graphic fallback. The mod does not have to name `ProceduralImage` itself.

The reference is compile-time only: `Plugins.dll` is never copied to the mod folder and never merged. The `DUCKY_SDK_GAMEUI` symbol is defined only when the file exists in the managed directory. Without the file, the build warns and falls back.

## 3. Radius tiers (conventions)

`GameUiMetrics.RadiusPanel` 16, `RadiusGroup` 11, `RadiusControl` 8, `RadiusTag` 6. **These are recommended conventions, not values measured from the game.** The only radius measured in the game is 22.5 on an `Alert` modifier. Radii are in 2560-reference design units and are passed through unchanged; the canvas scaler converts them.

## 4. Implementation practices and rules

- **Scale:** the scale is the screen width divided by 2560 (with the game's match of 0), so 1.125 at 2880 wide, 1.5 at 3840 wide, 0.75 at 1920 wide.
- **Main thread only.** Every Unity call in this guide (scaler, font, rounded graphics, text) must run on the Unity main thread, not in a thread-pool continuation. Use `await UniTask.SwitchToMainThread()` first.
- **No Unity access in static constructors or field initialisers.** A failure there poisons the type for the whole session. The font cache is filled only inside `GameUiFont.Get()`.
- **Lazy singletons.** Keep mod-level holders in the `BuffsContract` shape: find or create on first use and re-create if Unity destroyed them.
- **Event subscriptions.** Subscribe through `ModBehaviourBase.Subscriptions`, or remove every `-=` in `ModDisabled`. The GameUi classes subscribe to nothing.
- **Destroy what you create.** A panel created on enable must be destroyed on disable (keep a reference and destroy it in `ModDisabled`), otherwise hot reload leaves a copy behind.
- **Fallbacks:**
  - Font: `GameUiFont.Get()` returns the TMP default when the game font is unavailable.
  - Rounded graphics: `AddRounded` and `AddRim` fall back to `Image` and `Outline` when the game's rounded image is missing or fails to load. The first fallback in a session writes one warning.
  - `GameUiRounded.ForceFallback` is a diagnostic switch. Leave it false in shipped mods.
- **Shadow:** use the rounded sibling behind the panel. Do not use square `Shadow` on a rounded panel.

### Reuse the game's UI first

Ranked by effort (see section 1): call the game's own entry points, clone a live game prefab, build from `new GameObject` with the game's pieces, and hand-draw last.

## 5. Real-game verification

### 5.1 The sample

`Samples/Ducky.GameUiSample` is the verification vehicle. It sets `IncludeGameUi` true, scales its canvas on open, uses the game font and ladder sizes, builds a rounded panel with a gold rim and a rounded shadow sibling, and three buttons: a forced "selected" look, a "hover me" button, and a disabled button. `ModBehaviour.ForceFallbackCheck` is the switch for the fallback check. Its notes are in `Samples/Ducky.GameUiSample/VERIFICATION.md`.

### 5.2 Reading the canvas scale and text size with Newbe.DebugMod

Remote steps and the endpoints used are described in the `duckov-remote-debug` skill. The calls:

```
POST /api/v1/research/scan    {"type":"CanvasScaler","scope":"scene","limit":40}          # find HUDCanvas and the mod canvas ids
POST /api/v1/research/scan    {"type":"TextMeshProUGUI","scope":"scene","limit":200}      # find a game text and the mod text by name
POST /api/v1/research/inspect {"type":"TMPro.TextMeshProUGUI","id":<id>}                  # read fields.m_CanvasScaleFactor and fields.fontSize
POST /api/v1/commands/screenshot {"name":"ui"}                                            # screenshot; the response gives the host path
```

- The inspect response is large (the font asset is expanded). Read the one field you need from the JSON rather than printing the whole answer.
- Compare `m_CanvasScaleFactor` of the mod text with the same field of a text under `HUDCanvas` (for example `BulletCount`). Both must be 1.125 at 2880×1800.
- Ids change after a scene load or a restart: scan again before inspecting.

### 5.3 Comparing uppercase letter height

Take one screenshot that contains a game text and a mod text of the same font. Compare the height of an uppercase letter in both. Measured in pixels this is not yet done; the comparison so far is by eye.

### 5.4 Checking the fallback

1. Set `ForceFallbackCheck` to true in `ModBehaviour.cs`, hot reload with `hot-reload.sh` (do not redeploy a running game).
2. Expect: square `Image` panel, a square gold `Outline` on the panel, text and buttons fully visible, a brownish fill (the outline shows through the translucent fill).
3. Set the switch back to false and hot reload again: the rounded look returns.

### 5.5 Hot reload with the panel open

Each reload should unload the old Core (its panel is destroyed, `childObjects=0`) and load the new one (`childObjects=1`). `Player.log` must not contain `Exception` or `NullReference` lines for the mod.

### 5.6 Results recorded (2026-11-01)

| Check | Result |
| --- | --- |
| Canvas scale | Mod text `m_CanvasScaleFactor` 1.125; game HUD text `BulletCount` 1.125 (fontSize 18). Match. |
| Hot reload with panel open | Six reloads; each unload `childObjects=0`, each load `childObjects=1`; no exception lines. |
| Rounded look | Rounded panel and buttons, gold rim, text in the game font, ladder sizes. |
| Fallback look | Square `Image` and square `Outline`; no content covered; rounded look restored after switching off. |
| Fallback warning in `Player.log` | Not verified (see Known limitations). |
| Hovered button look | Not verified (see Known limitations). |

## 6. Known limitations

- **The SDK logger is a no-op in the game.** `Player.log` shows `[ModBehaviourBase] Failed to get a valid logger. Using NoOpLogger.` for every mod in this install, so `Log.*` output, including the fallback warning, does not reach the game log. Cause, confirmed at runtime: `DuckyDeveloperEditionSerilogLogProvider` looks up `Serilog.Log` with `Type.GetType("Serilog.Log, Ducky.Logging.DeveloperEdition")`, but in a hot-reload build the Serilog code is in the stamped Core `Ducky.Logging.DeveloperEdition.Core.<stamp>.dll`, not in the shim. Left as is for now.
- **Stale hot-reload Cores.** Each hot reload keeps the previous Core loaded (`coreAssembliesInDomain` grows with every reload). DebugMod's `research/reflect` and `research/invoke` resolve a type name to the first loaded copy, so a member added after the first load is reported as missing. A fresh launch loads only the newest Core.
- **Hover not verified.** The OS pointer is hidden in the game's aim mode, so no hovered button was captured.
- **Shadow not confirmed by eye.** The sibling is drawn behind the panel but is barely visible on the dark background.
- **Uppercase comparison by eye only.**

## 7. Observed facts still open (Not verified)

- No mod of ours had built a panel from `ProceduralImage` + `UniformModifier` + game font + `FadeGroup` before the sample. The sample now does the rounded panel and rim; the `FadeGroup` is not used.
- Panel colours and alpha were read only from screenshots (`research/inspect` returned an empty `fields` for the `Image`/`Graphic` base members).
- The sprite list covers loaded sprites only; a sprite that is not loaded in the base scene would not appear.
- `DialogueBubblesManager.Show` and the other views were not opened.
- Which assembly the compile-time reference for `ProceduralImage` uses is now decided: `Plugins.dll` (see section 2.6). `TeamSoda.Duckov.Core.dll` does not define the type.

## 8. How to repeat

`Newbe.DebugMod` deployed and the test save in place (see the `duckov-remote-debug` skill), then the calls in 5.2, the screenshot in 5.3 and the switch in 5.4. Copy the PNG down with `windows-host copy-from`, and restore the test save with `restore-save` when finished.
