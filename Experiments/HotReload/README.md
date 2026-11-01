# HotReload experiment (shim + Core)

Question: can a mod swap its own code in the running game, with the SDK providing the isolation layer?
Status: **verified in the real game** (remote Windows machine, 8 reloads, `run-remote.sh` PASS). Results below.

## Shape

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

## Facts read from the game's decompiled `ModManager` (Newbe.Dod/decompiles)

- Entry is `Assembly.LoadFrom(<mod>/<name>.dll).GetType("<name>.ModBehaviour")`, added with `AddComponent` on a new GameObject.
- `DeactivateMod` destroys that GameObject (so `OnDisable` runs); `ActivateMod` would `LoadFrom` the same path again.
- `ModBehaviour.Setup(ModManager, ModInfo)` is public, so the shim can hand its own `master`/`info` to the Core instance.

## What the experiment measures

`Probe` (in the shim, survives reloads) lets the shim check a reload left nothing behind. One line per stage in `Player.log`:

```
[HotReload] REPORT load=N stage=after-unload|after-load marker=<X|none> pingHits=<0|1> tickHandlers=<0|1> coreRunners=<0|1> coreAssembliesInDomain=<n>
[HotReload] loaded <file> as '<assembly full name>' sameAssemblyObjectAsEarlierLoad=<True|False>
```

Expected: after-unload `marker=none pingHits=0 tickHandlers=0 coreRunners=0`; after-load the new marker with all three `1`.
`coreAssembliesInDomain` growing by one per reload is the known leak (Mono cannot unload assemblies).

## Run

```bash
./build.sh B --stamp          # build + stage out/Ducky.HotReload (marker B, unique Core assembly name)
./run-remote.sh               # remote game: A; reload B C (unique names); D E (same name, E must NOT take effect); F G H (unique). Exits 2 if a game is running.
```

`run-remote.sh` reuses `Newbe.Dod/scripts/remote-test/remote-test.sh` steps. It edits the remote `Saves/Global.json`
(enables mod loading + this mod, original backed up) and removes the deployed mod folder at the end.

## Results (real game, Player.log in the run's artifact folder)

| Question | Answer |
| --- | --- |
| Does a Core loaded from bytes work in Unity's Mono (`AddComponent`, Harmony, event subscription)? | Yes. |
| Do `Ducky.HotReload` (the shim) and `0Harmony` resolve through the shim's `AssemblyResolve` handler? | Yes. |
| Is a reload clean? | Yes, 8 of 8: after every unload `pingHits=0 tickHandlers=0 coreRunners=0`, after every load the new marker with all three `1`. |
| Same assembly name loaded again (D then E)? | **No.** `Assembly.Load(byte[])` returns a new Assembly object (domain count still grows) but type lookup keeps returning the FIRST same-named assembly, so the OLD code ran (marker D, not E). First load of a plain name works; the second does not. |
| Unique name per build (`Core.<stamp>.dll`)? | Works every time, also after a same-name load. This is what the SDK build must do. |
| Leak? | One assembly stays in the AppDomain per reload (`coreAssembliesInDomain` 1..8), as expected for Mono. Dev only. |

Still open: only Harmony, event and GameObject cleanup were exercised. Things a real mod registers in game state (buff types via
`Contract.Buffs`, UI, localization) are not covered; `ModBehaviourBase` is not used by this experiment (Core uses the plain game base class).
