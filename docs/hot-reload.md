# Development hot reload

Hot reload lets a mod author change mod code while the game keeps running. It is **development only**: it is off by
default, it is ignored in Release builds, and a package that contains its files is refused.

Mono cannot unload an assembly, and the game loads a mod with `Assembly.LoadFrom(<mod>/<name>.dll)`, which returns the
assembly it already loaded for that path. Hot reload works around this with a small shim that the game loads, and a
Core assembly that the shim loads from bytes under a new name for every build.

## Enable it

Set one property in the mod project. It only takes effect in a non-Release configuration, and never for `IsModLib`
projects:

```xml
<PropertyGroup>
  <DuckyHotReload>true</DuckyHotReload>
</PropertyGroup>
```

| Configuration | `DuckyHotReload` | Result |
|---|---|---|
| Debug (or any non-Release) | `true` | Hot reload build: shim + stamped Core |
| Debug | not set / `false` | Normal single-assembly build (unchanged) |
| Release | `true` | Normal single-assembly build, with the warning *DuckyHotReload is ignored in the Release configuration* |
| `IsModLib` project | `true` | Normal build, with the warning *DuckyHotReload is ignored for library projects* |

The build prints `[Ducky.Sdk] Hot reload: development build (shim + Core)` when the mode is on.

The sample mods in `Samples/` that are deployed (`Ducky.TryHarmony`, `Ducky.SingleProject`,
`Ducky.MessageHubClient`, `Ducky.MessageHubHost`, `Ducky.MessageHubUI`, `Ducky.EntranceMod`, `Ducky.HotReloadSample`) set it
for non-Release builds, so their Debug builds are hot reloadable by default. The SDK default is still off.

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
- **MessageHub clients do not follow a host restart by themselves.** A reload stops the host that the old Core
  started and the new Core starts a new one. The new host has no client registrations, so a client that used the old host
  must register again after the reload. The sample does not include a client, so this was not observed in the game; it
  follows from the host holding its registrations in memory (`ModHttpV1`).
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
