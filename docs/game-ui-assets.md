# Building mod UI from the game's own UI assets

Research notes from a live session (2026-11-01) on the remote Windows machine, game language German, test save slot 6,
`Newbe.DebugMod` HTTP API (`research/scan`, `research/inspect`, `research/reflect`, `research/invoke`,
`open-view`, `commands/screenshot`). Everything under "Observed" was read from the running game; "Not verified" was not.

## Observed

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

- **Panels and slots are not sprites.** The dark translucent rounded panels, slot frames and buttons are
  `UnityEngine.UI.ProceduralImage.ProceduralImage` (third-party UI Procedural Image, compiled into the game; the string is
  in both `Managed/Plugins.dll` and `Managed/TeamSoda.Duckov.Core.dll`). Its corner shape comes from a modifier component:
  `UniformModifier` (one `Radius`, a `Alert` instance reads `22.5`) or `RoundModifier`; `BorderWidth` and `FalloffDistance`
  sit on the image. Reflection shows the members: `Modifier`, `ModifierType`, `BorderWidth`, `FalloffDistance`.
- **Views are a framework, not loose panels.** A `View` has a `FadeGroup` (`Duckov.UI.Animations`) whose elements are
  `RectTransformFade` / `CanvasGroupFade` children (`Title`, `Content`). `ItemCustomizeSelectionView` reuses
  `SlotDisplay(Clone)` prefab instances under a `GridLayout`. Views are looked up through `GameplayUIManager.GetViewInstance<T>()`.
- **Few real sprites are used for chrome.** Names that look like building blocks: `Rounded Rect`, `Rounded Rect small`, `Panel`,
  `UIMask`, `Frame_Basic_Rectangle`, `Rect16`, `Circle`, `MetalSlice`, `ToggleBox_On`, `ToggleBox_Off`, `Slider01`, `checkmark`,
  `arrow_back_24dp_FFFFFF_...` (the back arrow in the top-left of the workbench), `leftButton` / `middleButton` / `rightButton`,
  `HorizontalFrameMask_16_Left/Right`, `UpFrameMask16`, `DownFrameMask16`. Item icons (`Backpack_LV1`, `Card_*`, buff icons) are content, not chrome.
- **The font comes from the game.** `GameplayDataSettings.UIStyle.TemplateTextUGUI.font` is the TMP font the game's text uses.
  `Newbe.Dod` (`StandaloneEffectModal`, `EffectDetailModal`, `AdminPanel`) already reads it so its own panels look native.
  Ducky.Sdk exposes setters for it in `GameApis/UIStyleDataExtensions.cs`.
- **Ready-made entry points that work from a mod.**
  - `NotificationText.Push(string)` (static): observed in a screenshot, a black pill at the top centre (`Hello from probe`).
  - `Duckov.UI.DialogueBubbles.DialogueBubblesManager.Show(string text, Transform target, float yOffset, bool needInteraction, bool skippable, float speed, float duration)` (static, returns `UniTask`): signature read, not called.
  - Opening a native view: `ItemCustomizeSelectionView.Show()` produced the workbench screen (left panel "Deine Gegenstände", right empty detail card). Other kinds are in `Newbe.DebugMod/Features/ViewOpener.cs`.
- **How other mods reuse the game's UI.** `DuckMarket` (decompiled under `Newbe.Dod/decompiles/DuckMarket.Core/`) subclasses `View`
  and clones existing objects: `Object.Instantiate(ItemOperationMenu)` for its menu, `Instantiate(Button)` for extra buttons,
  `Instantiate(template, parent)` for list rows. `Ducky.MessageHubUI` in this repo builds everything from `new GameObject` with
  `Image` / `TextMeshProUGUI` / `ScrollRect`, no game assets, so it does not look like the game.

### What this means for building "similar" UI

Ranked by effort, cheapest first:

1. **Call the game's own widget** (`NotificationText.Push`, dialogue bubble, open a native `View`). No layout code.
2. **Clone a live game prefab instance** (`Instantiate` a `Button`, `SlotDisplay`, list-row template found under an existing view) and re-parent it. The look is exact, the risk is coupling to child names and to the view being loaded.
3. **Build from `new GameObject` but with the game's pieces**: `ProceduralImage` + `UniformModifier` for panels and buttons, the game TMP font from `UIStyle`, a `FadeGroup` for show/hide, and the few chrome sprites above. This is the route that works for a view the game does not already have, and it needs no sprite files in the mod.
4. Hand-drawn `Image` + own sprites (what `Ducky.MessageHubUI` does). Works, looks foreign.

## Not verified

- No mod of ours has yet built a panel from `ProceduralImage` + `UniformModifier` + game font + `FadeGroup` in the game. The claims above are from reading live objects and from other mods' code.
- Panel colours and alpha were not read (`research/inspect` returned an empty `fields` for the `Image`/`Graphic` base members); only a screenshot shows them (near-black, translucent).
- Sprite list covers loaded sprites only; a sprite that is not loaded in the base scene would not appear.
- Which assembly the compile-time reference for `ProceduralImage` should use (`Plugins.dll` vs `TeamSoda.Duckov.Core.dll`) was not decided; both contain the name.
- `DialogueBubblesManager.Show` and the other views were not opened.

## How to repeat

`Newbe.DebugMod` deployed and the test save in place (see the `duckov-remote-debug` skill), then for example:

```
POST /api/v1/research/scan    {"type":"Duckov.UI.View","scope":"scene","limit":40}
POST /api/v1/research/reflect {"type":"ProceduralImage"}
POST /api/v1/research/inspect {"type":"UniformModifier","id":-110532}
POST /api/v1/open-view        {"kind":"workbench"}
POST /api/v1/research/invoke  {"type":"NotificationText","method":"Push","a0":"hello"}
POST /api/v1/commands/screenshot {"name":"ui"}
```

Then `windows_host.py copy-from` the PNG. Run `restore-save` and `stop` when done.
