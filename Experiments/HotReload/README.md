# HotReload experiment (shim + Core)

Question: can a mod swap its own code in the running game, with the SDK providing the isolation layer?
Status: **builds locally; not yet run in the real game** (see "Not verified").

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
./run-remote.sh               # remote game: A, reload B C (unique names), reload D E (same name). Exits 2 if a game is running.
```

`run-remote.sh` reuses `Newbe.Dod/scripts/remote-test/remote-test.sh` steps. It edits the remote `Saves/Global.json`
(enables mod loading + this mod, original backed up) and removes the deployed mod folder at the end.

## Not verified (this is what the real game run must answer)

1. Does `Assembly.Load(byte[])` of a same-named assembly (D, E) return the old assembly (Mono dedup)? The log line `sameAssemblyObjectAsEarlierLoad` and the `marker` in the report answer it. If yes, use unique names (`--stamp`), which the SDK build would have to do per build.
2. Does Core (loaded from bytes) resolve `Ducky.HotReload` and `0Harmony` through the shim's `AssemblyResolve` handler?
3. Does `AddComponent` of a type from a bytes-loaded assembly work in Unity's Mono?
4. Do Harmony patches, event subscriptions and child objects really disappear on unload (the three `0` values)?

Mod-author cleanup still matters: anything the Core registers in game state (buff types, UI, game events) must be undone in `OnDisable`.
