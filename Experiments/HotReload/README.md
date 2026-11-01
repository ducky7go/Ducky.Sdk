# HotReload experiment (shim + Core) and hot reload validation

Questions:
1. Can a mod swap its own code in the running game, with the SDK providing the isolation layer? (shim + Core spike, verified earlier)
2. Which game registrations survive a reload of a `ModBehaviourBase` mod, and which game APIs remove them? (task 1 of `ducky-sdk-development-time-mod-hot-reload`)
3. Does the SDK's opt-in hot reload build (`DuckyHotReload`, see `../docs/hot-reload.md`) reload a real mod cleanly in the game? (tasks 6.2, 6.3, 7.1)

All real-game runs used the Escape from Duckov install on the remote Windows machine, driven by `Newbe.Dod/scripts/remote-test/remote-test.sh` through the windows-host skill.

## Shape (spike)

```
Mods/Ducky.HotReload/
  Ducky.HotReload.dll          shim: the only assembly the game loads (type Ducky.HotReload.ModBehaviour)
  Ducky.HotReload.Core[.stamp].dll   the reloadable mod code (type Ducky.HotReload.Core.CoreBehaviour)
  0Harmony.dll                 stable dependency, shared by every Core load
  hotreload.enabled            marker file: only then does the shim watch for a new Core dll
```

Shim reload step: `NotifyBeforeDeactivate` -> `Destroy(core GameObject)` -> wait 2 frames (Unity runs `OnDisable` at
end of frame) -> `Assembly.Load(File.ReadAllBytes(newest Core dll))` -> `AddComponent` -> `core.Setup(master, info)`.
`Assembly.Load(byte[])` is used, not `LoadFrom`, because `LoadFrom` returns the already loaded assembly for the same path.

## Spike results (plain game base class)

| Question | Answer |
| --- | --- |
| Does a Core loaded from bytes work in Unity's Mono (`AddComponent`, Harmony, event subscription)? | Yes. |
| Do `Ducky.HotReload` (the shim) and `0Harmony` resolve through the shim's `AssemblyResolve` handler? | Yes. |
| Is a reload clean? | Yes, 8 of 8: after every unload `pingHits=0 tickHandlers=0 coreRunners=0`, after every load the new marker with all three `1`. |
| Same assembly name loaded again (D then E)? | **No.** The OLD code ran. First load of a plain name works; the second does not. |
| Unique name per build (`Core.<stamp>.dll`)? | Works every time. This is what the SDK build does. |
| Leak? | One assembly stays in the AppDomain per reload (Mono cannot unload assemblies). Dev only. |

## Task 1: game facts for `ModBehaviourBase` (probe)

Probe: `Ducky.HotReload.BaseCore/` (the Core derives from `ModBehaviourBase` and compiles the SDK Lib sources in, as the
SDK does for mods), staged by `build-base.sh`, run by `run-probe.sh` on the remote game. The shim's `GameStateProbe`
counts the game-side state and removes it through game APIs (only when `probe-remove.enabled` exists in the mod folder).
Probe sequence: load A (plain) -> reload B (plain) -> reload C with removal in the shim after B's unload.

Kept evidence (run 4, the valid run): `results/probe-1-1-run4-lines.log` and `results/probe-1-1-run4-game-log.txt`.

### 1.1 What survives an unload, and which game API removes it

| Registration | After B unloads A (plain, no cleanup) | Removal API used | After removal |
| --- | --- | --- | --- |
| Buff registration (buff in `GameplayDataSettings.Buffs` list, registrar GameObject `BuffRegistrar_ProbeBuff`) | **survives**: `probeBuffs=1`, then `2` after B registers the same type again (duplicate) | `allBuffs.Remove(buff)` + `Destroy(registrar GameObject)` | `probeBuffs=0`, `probeRegistrars=0` (`locRemoved=True buffs=2`) |
| Buff ID region (`BuffIdRegion` in `ModsLocalConfig.json`, key = mod id) | persists, by design. With stamped Cores the mod id is stable (`local.Ducky.HotReload`) and the region stays `1530000` for B and C | none needed | — |
| Localized text (`LocalizationManager` override text) | **survives** (`loc=probe-A`); the new Core overwrites it (`loc=probe-B`) | `LocalizationManager.RemoveOverrideText(key)` returns `True` | `loc=none` |
| MessageHub host (`ModHttpV1` GameObject) | **survives** (`hostComponents=1`); the new Core does not start a second one, it connects to the existing host | `Destroy(host GameObject)` | `hostComponents=0`; the next Core starts a new host (`hostComponents=1`) |
| `BuffsContract` holder (`<modId>_BuffsContractHolder` GameObject) | **survives** (`buffHolders=1`); the new Core cannot reuse its component (the type is from the old assembly), so a second holder with the same name would be created | `Destroy(holder GameObject)` | `buffHolders=0` |
| Buff type | cannot be unloaded (Mono). Only its registrations can be removed | — | documented in `../docs/hot-reload.md` |

### 1.2 How the build treats `0Harmony.dll` (code reading, `repos/Ducky.Sdk/Sdk/Ducky.Sdk.Build`)

- `Ducky.Sdk.props` adds `0Harmony` as a `Private=true` reference when `IncludeHarmony=true`, so `0Harmony.dll` lands in
  the build output (`bin/…`).
- `UpdateBuildContextAfterBuildStep.UpdateDependencies` (`Steps/UpdateBuildContextAfterBuildStep.cs`, lines 85–101) puts
  every DLL of the output folder that is not the main assembly and not a managed DLL into `DependencyAssemblies`. So a
  `0Harmony.dll` in the output **is a merge input** of ILRepack (`Libs/ILRepackAssembliesLib.cs`, line 95).
- The exclusion list with `0Harmony.dll` (`Libs/CopyDependenciesLib.cs`, line 241) applies only to the copy of the
  `Dependency` folder. It does not stop the merge.
- Result: before this change, a mod with `IncludeHarmony=true` got Harmony merged into its single DLL. In hot reload builds
  `IsHotReloadOnlyFile` (`Steps/UpdateBuildContextAfterBuildStep.cs`) keeps `0Harmony.dll` and the older Cores out of the
  merge. Verified on the built sample: the Core has 0 `HarmonyLib` types and references `0Harmony` as a separate assembly.

### 1.3 `MessageHubManager` stop operation and client behaviour (code reading)

- `MessageHubManager` had **no stop operation**: only `IsHostRunning`, `StartHost` and `GetCurrentHost`. This change adds
  `StopOwnedHost` (only for the host the same assembly started, see `Contracts/ModProtocols/HostOwnership.cs`).
- `IsHostRunning` finds a host by GameObject name (`ModHttpV1`) and by component type name, so a new Core connects to an
  old host unless the old host is destroyed first. That is what the probe shows.
- Clients: `ModHttpV1.RegisterClient` stores the client's callback in the host instance's `_eventMap`. The map is lost when
  that host is destroyed, so a client must register again after a host restart. **Not observed in the game** (the sample
  has no client); documented in `../docs/hot-reload.md`.

### Found while probing (fixed in the SDK)

- **A Core loaded from bytes has no `Assembly.Location`.** `Helper.GetModId()` then threw `ArgumentException: Invalid path`
  (`GetDirectoryName("")`), and `ModBehaviourBase.OnEnable` aborted before `ModEnabled` ran (run 2 log). Fixed:
  `Helper.GetModFolderName` takes the folder from the already loaded shim of the same mod, and the mod name drops the
  `.Core.<stamp>` suffix.

## Task 6/7: the SDK hot reload sample in the real game

Mod: `Samples/Ducky.HotReloadSample` (built by the SDK with `DuckyHotReload=true`, Debug). It has a Harmony patch, an event
subscription, a child GameObject, a Buff, a localized text, a Canvas and the MessageHub host, and it cleans up in
`ModDisabled`. Its `HotReloadReport()` returns its own counts (patches, subscriptions, UI, localized text).

Command: `./run-remote.sh sample`. Steps: build the sample, stage its `Publish` folder (shim, `0Harmony.dll`, marker,
Core), deploy, enable, launch, then 8 unique-name reloads (each one a fresh build), then a same-name Core.

Final run: **PASS** (exit code 0, 8 reloads, one same-name refusal). Log lines: `results/sample-final-run-lines.log`.

| Load | Core | After-unload of the previous Core | After-load of this Core |
| --- | --- | --- | --- |
| 1 | `Ducky.HotReloadSample.Core.20261101044857026` (first build) | (initial) | `childObjects=1 hostComponents=1 buffs=1 samplePatches=1 sampleSubscriptions=1 sampleUi=1 sampleHolders=1 sampleLocalization=sample <this Core>` |
| 2–9 | one new Core each | `childObjects=0 hostComponents=0 buffs=0 samplePatches=0 sampleSubscriptions=0 sampleUi=0 sampleHolders=0 sampleLocalization=none` | same as load 1, with this Core's name |

`sampleHolders` counts this mod's buff holder only (`<modId>_BuffsContractHolder`); other mods keep their own holders,
which the count ignores.

- After every unload all counts are zero; after every load each count is exactly one (8 reloads; 9 loads in all).
- Same name: the Core that was just loaded was written again under the same name, and it was refused with
  "already loaded in this session … the old code keeps running" and never reloaded.
- Harmony stayed one file: the deployed `0Harmony.dll` was not rewritten between builds (unit test and the sample build).
- The Core's entry type has the same full name as the shim's entry type (`Ducky.HotReloadSample.ModBehaviour`). The
  REPORT counts come from the Core (its `HotReloadReport`), so the Core's type was the one created (task 4.5).

Earlier attempts (not kept as results): run 1 had a regex that did not match the CRLF log lines (the reports
themselves were right; the regex was fixed); run 2 was interrupted when the game session ended at 21:31 (no crash in
the Windows event log; the session was not started by this run, and the run's own `stop` found no recorded game), so
its reload numbering could not match; run 4 counted every buff holder in the game, which includes the holders of other
mods, so its holder check failed although the Core's holder was created and destroyed once per reload (the counts were
flat across reloads). Run 5 is the one kept.

Remote state after the runs: the game was not running, the test mod folder was removed, the shared
`ModsLocalConfig.json` was restored (only the two original Newbe entries), and save slot 6 was restored.

## Build and test evidence

- `Sdk/Tests/Ducky.Sdk.Lib.Tests`: 268 passed (baseline 211 + 57 hot reload tests), Debug configuration. The Release
  configuration cannot build the test project on this machine (no SixLabors license key for ImageSharp.Drawing; the
  license check is an error in Release only).
- Sample builds (Debug): dev output is `Ducky.HotReloadSample.dll` (shim), one `Ducky.HotReloadSample.Core.<stamp>.dll`,
  `0Harmony.dll`, `hotreload.enabled`, `info.ini`, `preview.png`. A second build replaces the Core and keeps the Harmony file
  unchanged. Release output is a single `Ducky.HotReloadSample.dll` with a warning. `EnableILRepack=false` gives the same
  set without a merge.
- Default builds of all eight other samples (`Samples/*`) before and after the change produce identical output file lists
  (59 files in total), checked by building each sample against the old and the new package.

## Still open (task 7.4)

| Item | Result | Reason it is not closed |
| --- | --- | --- |
| Buff registrations | closed: removed (`buffs=0` after unload, `1` after load) | — |
| Buff types | limitation, documented | Mono cannot unload a type; only its registrations are removed. |
| UI | closed: the sample's Canvas (`sampleUi`) | — |
| Localized texts set with `LocalizationManager.SetOverrideText` by the mod | closed: the sample's key is removed in `ModDisabled`; the text changes on every load | — |
| MessageHub host | closed: stopped by the owner, one host after each load, none of the old Core after each unload | — |
| Buff holder (`BuffsContract` holder object) | closed: one holder of this mod after each load, none after each unload | — |
| SDK-managed localized keys (`LK`/CSV keys set by `LocalizationService`) removed on a hot reload disable | **open** | The sample sets its text directly, so the SDK's key release path (`LocalizationService.ReleaseOverrideTexts`) is unit-untested and not exercised in the game. |
| Logger after reload (`Log.Current`) | **open** | The game's `LogProvider` returns `NoOpLogger` for these mods (`[ModBehaviourBase] Failed to get a valid logger`), so a Core's log lines cannot be seen in `Player.log`. |
| MessageHub clients after a host restart | **open**, documented as a limitation | The sample has no client. The code says a client must register again. |
| Release disable keeps the host, and re-enable does not start a second one (task 5.3) | **open** | The test harness has no way to disable and re-enable a mod in a release build. The decision rule is unit tested. |

## Accepted limitations (decided by the user, tasks 3.3, 4.2, 5.3, 5.5, 5.6)

The user chose to accept these as documented limitations and tick the tasks. The rows above that say **open** remain
open; these five are closed by acceptance, not by a test.

| Task | Accepted limitation | What was checked instead |
| --- | --- | --- |
| 3.3 Harmony not merged into a Core | No committed test. The check is manual. | One-off metadata check of the built sample Core: 354 types, 0 `HarmonyLib` types, `0Harmony` referenced as a separate assembly; the merge-disabled build gives the same shim/Core/Harmony set. Deployed `0Harmony.dll` is unchanged across builds (unit test and sample build). |
| 4.2 Reload state machine, retry on a locked or partial file | The retry path inside the Unity coroutine (`Reload`) is not exercised by a unit test or the game run. | The decisions (newest Core, stable window, same-name rejection, no overlap) are unit tested in `HotReloadScheduleTests`; destroy → two frames → load → `Setup` and the report sequence are verified by the game run. |
| 5.3 Release path: host keeps running, re-enable starts no second host | Not exercised in the game (no disable/re-enable trigger in the harness). | `ShouldReleaseHotReloadState(false, …)` is unit tested: release builds never stop the host or release buffs/localization. |
| 5.5 Logger, enable state, localized string after reload | The logger cannot be observed: the game returns `NoOpLogger` for these mods. | The new Core's localized string changes per load and its `ModEnabled` runs (the sample's checks). |
| 5.6 `ModDisabled` checklist assertions | The buff-type item has no assertion: Mono cannot unload types. | Buff registrations are asserted (`buffs=0` / `1`); the checklist in `docs/hot-reload.md` states the type limitation. |

