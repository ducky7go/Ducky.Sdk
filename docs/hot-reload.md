# Development hot reload

Hot reload lets a mod author change mod code while the game keeps running. It is **development only**: it is on by
default for the Debug builds of a mod that can use it, you can opt out with `DuckyHotReload=false`, it is ignored in
Release builds, and a package that contains its files is refused.

Mono cannot unload an assembly, and the game loads a mod with `Assembly.LoadFrom(<mod>/<name>.dll)`, which returns the
assembly it already loaded for that path. Hot reload works around this with a small shim that the game loads, and a
Core assembly that the shim loads from bytes under a new name for every build.

## Default on, opt out

Hot reload needs no configuration. A Debug (any non-Release) build of a mod project is a hot reload build when the
project can produce one, which means all of these hold:

- the configuration is not Release;
- the project is not an `IsModLib` library;
- the project declares a `ModName`;
- the project is deployed (`DeployMod` is not `false`).

Every other project builds as a normal single assembly, with no hot reload file and no hot reload warning.

To opt out, set the property to `false` in the mod project (or in a shared props file that the mod imports):

```xml
<PropertyGroup>
  <DuckyHotReload>false</DuckyHotReload>
</PropertyGroup>
```

| Configuration | Project | `DuckyHotReload` | Result |
|---|---|---|---|
| Debug (or any non-Release) | deployed mod (`ModName` set, `DeployMod` not `false`) | not set / `true` | Hot reload build: shim + stamped Core |
| Debug | deployed mod | `false` | Normal single-assembly build (opt-out, no warning) |
| Debug | no `ModName`, or `DeployMod=false` | not set / `true` | Normal single-assembly build, no warning |
| Release | any | not set / `false` | Normal single-assembly build, no warning |
| Release | any | `true` | Normal single-assembly build, with the warning *DuckyHotReload is ignored in the Release configuration* |
| `IsModLib` project | any | not set / `false` | Normal single-assembly build, no warning |
| `IsModLib` project | any | `true` | Normal build, with the warning *DuckyHotReload is ignored for library projects* |

The warnings appear only when the author sets `DuckyHotReload` to `true` explicitly. A project that gets hot reload by
default is never warned about it.

The build prints `[Ducky.Sdk] Hot reload: development build (shim + Core)` when the mode is on.

A normal build does not remove the Cores and `hotreload.enabled` that an earlier hot reload build left in the mod folder.
After you opt out, delete `hotreload.enabled` and the `<ModName>.Core.*.dll` files from the mod folder, or the release
packaging guard refuses the package (see Packaging).

The deployed sample mods in `Samples/` (`Ducky.TryHarmony`, `Ducky.SingleProject`, `Ducky.MessageHubClient`,
`Ducky.MessageHubHost`, `Ducky.MessageHubUI`, `Ducky.EntranceMod`, `Ducky.HotReloadSample`) use the SDK default, so their
Debug builds are hot reloadable with no property set. `Ducky.EntranceMod2` (`DeployMod=false`) and
`Ducky.EntranceMod.Common` (`IsModLib`) build normally.

## What a hot reload build writes

For a mod named `Ducky.HotReloadSample` the mod folder holds (this is the real output listing of a dev build):

```
Ducky.HotReloadSample/
  Ducky.HotReloadSample.dll                       shim: the only assembly the game loads (type Ducky.HotReloadSample.ModBehaviour)
  Ducky.HotReloadSample.Core.20261101042636891.dll  the mod's code, one per build; the number is the build stamp
  0Harmony.dll                                    the SDK's Harmony, beside the shim, never merged into a Core
  hotreload.enabled                               marker: the shim watches for new Cores only while it exists
  info.ini, preview.png, assets...                as in every mod
```

- **Stamp.** Each build names its Core `<ModName>.Core.<stamp>`, where the stamp is the UTC time with milliseconds
  (`yyyyMMddHHmmssfff`). The name inside the file equals the file name. Two builds never share a Core name.
- **Stale Cores.** A build deletes the older `<ModName>.Core.*.dll` files (and their PDBs) from the output folder and
  the deployed mod folder, so exactly one Core remains. A file the game holds open cannot be deleted: the build warns
  and continues, and the shim loads the newest Core by stamp.
- **Harmony.** `0Harmony.dll` is copied beside the shim once. It is rewritten only when its content changes, and it is
  never merged into a Core. A Core that uses Harmony resolves it from that file.
- **Merging.** With `EnableILRepack` (the default), the mod's own dependencies are merged into the Core. With
  `EnableILRepack=false`, they are copied beside the shim (the `Dependency` folder), and the shim resolves them there.
- **Marker.** `hotreload.enabled` is written only in hot reload builds. Remove it to stop reloads while the game runs.

Normal builds write none of these files, and the output of a normal build is the same as before hot reload existed.

## How a reload works

1. Build the mod. Every build writes a new Core, deletes the older ones, and the shim is unchanged.
2. The shim polls the newest Core file every half second, while `hotreload.enabled` exists.
3. A new Core is loaded only after its file has stopped changing for one second (a copy in progress is not loaded).
4. The shim reads the new bytes first. A locked or partial file is retried, and the running Core keeps running.
5. The shim destroys the old Core's object, waits two frames so the old `ModDisabled` cleanup has run, and then loads
   the new Core from bytes, adds its behaviour and calls `Setup`.
6. A reload never overlaps another one.

A Core whose assembly name is already loaded in this game session is **refused**: the shim logs that same-name reloads
are unsupported, and the old code keeps running. Restart the game to load such a build. The build's unique stamp
prevents this for normal builds.

The shim writes one `REPORT` line for every reload stage (`after-unload` and `after-load`). It carries the Core
assembly count in the AppDomain, the shim's child objects, the MessageHub host components, the game buffs that come from
any Core of the mod, and the Core's own counts (`Ducky.Sdk` mods report their Harmony patches, event subscriptions and
localized texts). After an unload every count must be zero; after a load each count must be exactly one.

## What you must clean up in `ModDisabled`

`ModDisabled()` runs when the old Core is destroyed. The table says what each item needs.

| Item | What you do | What the SDK does in a hot reload build |
|---|---|---|
| Harmony patches | Call `UnpatchAll(yourId)` (or unpatch your own methods). | Nothing. |
| Event subscriptions | Unsubscribe every handler you subscribed with `-=`. | Nothing. |
| GameObjects you created | Destroy them (a child of your behaviour is destroyed with it). | Nothing for your own objects. |
| Buff registrations (`Contract.Buffs.RegisterBuff`) | Nothing. | Removes every buff this Core registered from the game's buff list and destroys its registrar object, and destroys the buff holder this Core created. |
| Buff types | Nothing. A buff type cannot be unloaded: the type stays in the old assembly, and its registrations are removed above. | — |
| UI | Destroy the UI objects you created. | Nothing. |
| Localized texts set by the SDK (`LK` keys, CSV files) | Nothing. | Removes every key this Core set through the SDK. |
| Localized texts you set yourself with `LocalizationManager.SetOverrideText` | Call `LocalizationManager.RemoveOverrideText(key)`. | Nothing. |
| MessageHub host | Nothing. | Stops the host if this Core started it. The next Core starts a fresh host. A host started by another mod is left running. |

In a release build none of the SDK steps above run: the MessageHub host keeps running after a mod is disabled, as it
always did.

A mod that does not clean up in `ModDisabled` shows duplicated or stale behaviour after a reload.

## Shared libraries

A library project the mod references (a `ProjectReference`, for example `Ducky.EntranceMod.Common`) is covered by hot
reload only when merging is on (`EnableILRepack`, the default):

- **Merged (default).** ILRepack merges the library into the Core, so its types live in
  `<ModName>.Core.<stamp>.dll`. No separate library DLL is deployed to the mod folder. Every reload is a new Core, so
  the library's code and its static state are reloaded with it. The SDK's own `Ducky.Sdk.Lib` source is compiled into
  the Core the same way. Checked on `Ducky.EntranceMod`: its Core contains the `Ducky.EntranceMod.Common` types, and the
  mod folder has no `Ducky.EntranceMod.Common.dll`. The game run passed for the mod as a whole; the library's own logic
  was not tested separately.
- **Not merged (`EnableILRepack=false`).** Hot reload does not cover shared libraries in this mode, and the mod can fail
  to load:
  - The library DLL stays in `bin` and is not deployed. The Core cannot find it at runtime. Checked by a build: only
    `bin/Debug/netstandard2.1/Ducky.EntranceMod.Common.dll` exists, not in the mod folder. The cause is that the existing
    dependency copy only reads `bin/<Configuration>/<TargetFramework>/Dependency`, so project reference outputs are never
    copied. This was already the case before hot reload. Not verified in the game.
  - Even a library copied into the `Dependency` folder is loaded by the shim with `Assembly.LoadFrom`. A path is loaded
    only once, so the library is not reloaded: its code and static state persist, and changes to it do not take effect.
    This follows from the loading code. Not verified in the game.

Keep `EnableILRepack` on for a mod you want to hot reload. With it off, do not rely on hot reload.

## Limits

- **Logging from a reloaded Core is not observed.** In the game the SDK's logger for these mods is a no-op
  (`Failed to get a valid logger`), so log lines of a Core cannot be checked in `Player.log`. Use `Debug.Log`, which the
  game does write, when you need evidence of a reload.
- **Retry on a locked or partial Core file** is implemented in the shim but not covered by an automated test. The
  reload decisions around it are tested.
- **Release disable and re-enable** of the MessageHub host is covered by a unit test of the decision rule, not by a game
  run.
- **Memory grows with each reload.** Mono keeps the old Core assembly loaded until the game restarts. The shim logs the
  Core assembly count in every `REPORT` line.
- **Same-name Cores are unsupported** and refused (see above). Unique stamps are mandatory, and the build provides them.
- **The shim itself is not reloaded.** A change to the shim (the SDK's template) needs a game restart.
- **Harmony is not reloaded.** `0Harmony.dll` is the same assembly for every Core. Patches are applied and removed per Core.
- **Library projects (`IsModLib`) are not reloaded.**
- **MessageHub clients of the SDK follow a reload; clients a mod author registered directly do not.** A reload stops the hub
  that the old Core started and the new Core starts a new one, with no registrations. The SDK's own hub clients (the HTTP
  endpoint client, the debug event reporter and the client contracts) find the live hub object again, register on it once
  and, for the endpoint client, announce their endpoints again, all without a game restart. A MessageHub client that a mod
  author registered directly on `ModHttpV1` is not tracked by the SDK, so it must register again after the reload. The
  hub holds its registrations in memory, so that is the only way it comes back. The scenarios are in
  `Sdk/Tests/Ducky.Sdk.Lib.Tests/HotReloadScenarioTests.cs`. The real-game counts are in "Hub clients in the real game" under Verification.
- **Game state outside the mod** is reloaded only as far as the list above says.
- **Shared libraries** are reloaded only with merging on. See "Shared libraries" above.

## Packaging

A package must not carry hot reload files. `Ducky.Sdk.Validation.targets` fails the `Pack` target when `hotreload.enabled`
or a `<ModName>.Core.*.dll` is in the mod folder or the output folder, so a development build cannot be packaged by
mistake. Build the release with `Configuration=Release` (without `DuckyHotReload`) to package it.

## Verification

The mechanism was verified in the real game (Escape from Duckov, remote Windows machine) with
`Experiments/HotReload/run-remote.sh sample`. The sample mod (a Harmony patch, an event subscription, a child object, a
Buff, a localized text, a Canvas and the MessageHub host) was loaded once and reloaded eight times with unique names, and
a Core whose name was already loaded was refused. Every unload left zero of each item and every load left exactly one.
The results and the log lines are in `Experiments/HotReload/README.md` and `Experiments/HotReload/results/`.

### Hub clients in the real game (HTTP endpoints and debug events)

Run on the remote Windows machine with Escape from Duckov (Debug builds, local `Ducky.Sdk` package). Four samples were
loaded in one game session: `Ducky.HttpEndpointSample` and `Ducky.DebugEventReporter` (the reloaded client Cores),
`Ducky.HttpHostSample` and `Ducky.DebugEventRecorder` (kept running). The game was started once and not restarted. Then
the two client samples were rebuilt and reloaded eight times in a row (16 hot reloads in all). Each reload was confirmed
by the shim's `REPORT ... stage=after-load` line, and every reload command exited with 0.

| Check | Result after each of the 8 rounds | Observed |
| --- | --- | --- |
| Host route listing (`127.0.0.1:18765/httphost/routes`) | 3 routes, each once, owner `local.Ducky.HttpEndpointSample` (`GET /status`, `GET /frame`, `POST /echo`) | 8 of 8 rounds |
| `GET /local.Ducky.HttpEndpointSample/status` | names the newest Core (`core` = that round's `Ducky.HttpEndpointSample.Core.<stamp>`) | 8 of 8 rounds |
| Recorder file, `core-enabled` lines | one per enable, detail = Core assembly name, all names distinct | 9 of 9 (the first load + 8 reloads) |
| Recorder file, `multiline` lines | one per Core | 9 of 9 |
| Recorder file, `tick` lines per Core | counts 1..N with no gap and no repeat, per Core | 9 of 9 Cores (216 ticks in all) |
| Recorder file, exact duplicate lines | none | 0 |
| Recorder file, `burst` lines | none: the 1000-event burst reported in `ModEnabled` is discarded while the reporter is Idle | 0 of 9 enables |
| Exception lines in `Player.log` | none | 0 |

Not observed in the game: the host sample's `RoutesChanged` log line. The sample's `Log` output does not reach
`Player.log` in these mods (the game returns `NoOpLogger`), so only the route listing shows the route table. The
endpoint client's announce after each reload was seen through the route table and `/status`, not through a log line.


### Hub replacement and host restart (real game)

A second run in one game session, with the same four samples. `Ducky.DebugEventRecorder` started the hub (its REPORT lines
show `hostComponents=1` after load and `hostComponents=0` after unload), so reloading it replaces the hub that the other
samples use. The host sample (`Ducky.HttpHostSample`) and the endpoint client were not reloaded for the first three steps.

| Step | Route listing (`/httphost/routes`) | `/status` through the host | Notes |
| --- | --- | --- | --- |
| Baseline | 3 routes, each once | 200, the endpoint Core answered | |
| Recorder reload 1, 2, 3 (hub replaced each time) | 3 routes, each once, after every replacement | 200 each time | the endpoint client and the host both bound to the new hub; the request was forwarded through the new hub |
| Host reload (`Ducky.HttpHostSample`) | empty | 404 | the host restarted within one check interval, so the endpoint client did not notice (the documented limitation) |
| Endpoint client reload | 3 routes, each once | 200, the new Core answered | the documented recovery path |

Recorder file, same session: one reporter Core, ticks 1 to 141, no duplicate tick. One tick is missing between
04:03:04.5 and 04:03:06.5 (UTC), in the window of the second recorder reload. No `debug-events-dropped` line was written
for it: the reporter discards events while it is Idle and does not count them, so the loss is not recorded. No tick
was written twice.

Not verified in this run: the host sample's `RoutesChanged` log line (not visible in `Player.log`, see above).
