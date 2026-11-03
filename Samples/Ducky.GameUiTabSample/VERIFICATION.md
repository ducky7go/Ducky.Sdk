# Ducky.GameUiTabSample: real-game verification

Windows 11, Escape from Duckov (windowed, 2880×1800), test save slot 6, base scene, Hagicode.DebugMod and this mod loaded (`Mod Loaded:` lines in `Player.log`, no exception lines naming either). SDK package `0.0.300201-dev` (local feed). 2026-11-03.

| Check | Result |
| --- | --- |
| Tab key (real `keybd_event`) opens the bar with a 7th tab | Pass (screenshot) |
| Click the new tab: warehouse closes, panel opens, tab highlighted | Pass |
| Click the map tab: panel closes, map opens | Pass |
| Panel lists vitals and all 71 stats of the character item | Pass |
| "Copy brief text": status shows `Copied 290 chars`; clipboard holds the brief text (sentinel value set first) | Pass |
| Esc closes the panel, HUD returns | Pass |
| Hot reload with the panel open, three times | Pass: unload `childObjects=0`, load `childObjects=1`, no exception |

Brief text read from the clipboard:

```
[Duckov] Player brief
HP 77/77  Energy 105/105  Water 105/105  Stamina 104/104  Money 73,996
Walking Speed 4, Running Speed 7.8, Max HP 77, Max Weight 82, Stamina 104, Gun DMG Multiplier 77.3 %, Reload Speed 5.0 %, Gun CRIT Chance 0.0 %, Backpack Capacity 52, Melee DMG Multiplier 131.3 %
```

Not verified: entering a level (tab bar rebuilt), other resolutions, a custom `Icon` sprite. Details and the observed layout: `docs/game-ui.md` section 9.
