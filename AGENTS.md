# Ducky.Sdk - AI Coding Agent Instructions

## Project overview

Ducky.Sdk is the NuGet package `Ducky.Sdk` (MIT, author Newbe36524, repo `ducky7go/Ducky.Sdk`) for building C# mods for the Unity game "Escape from Duckov". A mod author adds one `PackageReference` and gets: shared library source compiled into the mod, a Roslyn localization generator and Unity analyzers, an MSBuild pipeline that deploys the mod to the game folder, generated `info.ini` / `preview.png`, ILRepack merging into one DLL, hot reload for development builds, and optional Harmony (`0Harmony.dll`).

It is a `developmentDependency`. The library ships as **source** (contentFiles), not as a compiled assembly.

Docs: `README.md` (Chinese), `README_en.md` (English), and `docs/` (`hot-reload.md`, `localization.md`, `debug-events.md`, `game-ui.md`, `http-endpoints.md`, `game-mechanics.md`, `localization-dod-migration.md`).

Behavior or spec changes follow the OpenSpec workflow.

## Repository layout

- `Sdk/Docky.Sdk.slnx` - solution (note the `Docky` spelling): Analyser, Lib, package project, build runner, tests.
- `Sdk/Ducky.Sdk.Analyser/` - `netstandard2.0` Roslyn project.
  - `DuckyLocalizationGenerator.cs`: incremental generator for the `LK` -> `L` pattern.
  - `UnityDebugAnalyzer.cs` (`DUCKY001`-`DUCKY005`, `Debug.Log*` calls) and `UnityDebugCodeFixProvider.cs`.
  - `UnityThreadingAnalyzer.cs`: `DUCKY006` (Unity API inside `Task.Run` / `UniTask.RunOnThreadPool` delegates) and `DUCKY007` (Unity access in static constructors and static initializers).
- `Sdk/SDKlibs/Ducky.Sdk.Lib/` - `netstandard2.1` source library. Needs the game's `Managed` DLLs to compile.
  - `ModBehaviours/` (`ModBehaviourBase`, `HagiCodePromotion.cs`), `Localizations/` (`L`, `LocalizationService`, `CsvParser`), `Logging/` (LibLog-style; `LogProviders/BmlSerilogLogProvider.cs` looks for the `Newbe.BetterModLoader` assembly), `Options/` (`ModOptions`), `Attributes/`, `Utils/`.
  - `Contracts/`: ModHttpV1 message hub (`ModProtocols/`, `ModHttpV1.cs`, `MessageHubManager.cs`, `ModHttpV1Proxy.cs`), `ModTerminalClientContract.cs` (targets the `TerminalUIMod` mod id, see the `Ducky.Terminals` repo), `DebugEvents/`, `HttpEndpoints/` (client and host core), vendored `System.CommandLine/`, `BuffsContract.cs`.
  - `GameApis/` (extension methods over game types), plus `GameUi/`, `GameEvents/`, `Threading/`, `Ai/`, `Combat/`, `Economy/`, `World/`, `Rules/`, `Control/`.
- `Sdk/SDKlibs/Ducky.Sdk/` - the package project.
  - `Ducky.Sdk.csproj` packs through `Ducky.Sdk.nuspec` (tracked in git).
  - `Ducky.Sdk.props` holds the defaults. `Ducky.Sdk.targets` imports `Ducky.Sdk.Validation.targets` first, then `Ducky.Sdk.Orchestration.targets`.
  - `HotReload/` (shim template and schedule), `0Harmony.dll`.
- `Sdk/Ducky.Sdk.Build/` - build runner, a `net10.0` console app (`Ducky.Sdk.Build.dll <build-context.json> --phase pre|post`).
  - `Libs/*Lib.cs`: one per build step (localization key extraction and CSV update, `info.ini`, preview, deploy, ILRepack in-process via `ILRepackAssembliesLib.cs`, dependency copy, ...).
  - `Shared/` (`BuildContext`, `BuildResult`, `BuildStamp`, `DeployManifest`, `HotReloadFiles`), `Steps/` (build-context generation, hot reload shim and cleanup, build-context update after build), `Runner/` (`BuildRunner.cs`, `RunnerErrors.cs`), `RunnerExitCodes.cs`.
  - Published by `packToLocal.sh` to `Sdk/Ducky.Sdk.Build/publish/net10.0/` and packed under `tools/net10.0/`.
- `Sdk/Tests/Ducky.Sdk.Lib.Tests/` - NUnit + Shouldly tests. `BuildRunner/` covers the build steps and the runner end to end; `Golden/` holds golden files (for example `ducky-build-context.golden.json`). The csproj references Lib, `Ducky.Sdk.Build` and the Analyser.
- `Samples/` - sample mods (`Docky.Sdk.Sample.slnx`): `Ducky.SingleProject`; `Ducky.EntranceMod` + `Ducky.EntranceMod2` + `Ducky.EntranceMod.Common` (multi-project, `IsModLib`); `Ducky.TryHarmony`; `Ducky.MessageHubHost` / `Client` / `UI`; `Ducky.HotReloadSample`; `Ducky.HttpEndpointSample`; `Ducky.HttpHostSample`; `Ducky.DebugEventRecorder` / `Reporter` / `SecondRecorder`; `Ducky.GameUiSample`; `Ducky.GameUiTabSample`. They are SDK integration tests, not templates. Sample mods that consume the published package live in the `ducky7go/Ducky.Samples` repo.
- `scripts/` - `packToLocal.sh`, `rebuild_samples.sh`, `fetch_build_dependency.sh`, `generate_preview_backgrounds.sh`, `comfyui/`.
- `duckylocal/` - local NuGet feed (its contents are git-ignored; only `placeholder.txt` is tracked). `nuget.props` holds `LocalNuGetVersion`. Also `GitVersion.yml` and `.config/dotnet-tools.json` (no tools).

## Build / test / run

- Prerequisites: .NET SDK 10. Root `global.json` pins 10.0.402 with `rollForward: latestFeature`, and CI uses that file. The build runner runs on the .NET 10 runtime that the SDK ships with. No `dotnet tool restore`, `dotnet-script` or `ilrepack` install is needed.
- Lib targets `netstandard2.1` because the game runs on Unity's Mono runtime. .NET 10 is only the build toolchain (see "Unity runtime constraints").
- The game's `Managed` assemblies are not in git. `scripts/fetch_build_dependency.sh` downloads the latest release of `ducky7go/sdk_build_dependency` into `./Managed/` (git-ignored). It needs network, so do not run it casually. The build prefers `<repo root>/Managed/` when that folder exists, otherwise the game install from `SteamFolder` (see Gotchas).
- Unit tests (as CI): `dotnet test Sdk/Docky.Sdk.slnx --configuration Release`.
- Pack to local feed: `./scripts/packToLocal.sh [--version X] [--configuration Debug|Release] [--skip-tests] [--no-build] [--purge-all-versions] [--no-clear-cache] [--clear-all-caches] [--max-packages N] [--no-cleanup]`.
  - It builds the Analyser, builds the package project, publishes the build runner, runs tests (failures only warn), then runs `dotnet pack` with a temp nuspec into `duckylocal/`.
  - Without `--version` it auto-increments `nuget.props` (`0.0.XXXXXX-dev`). It keeps the latest 20 packages by default (`--max-packages`).
- End-to-end check: `./scripts/rebuild_samples.sh`. It packs, rewrites the `Ducky.Sdk` version in every sample csproj with `dotnet add package`, then builds `Samples/Docky.Sdk.Sample.slnx`. CI runs this after the tests.
- Run `dotnet test` yourself for a reliable test result (see Testing status).

## CI and release

- `build_dotnet.yml`: on push and pull request to `main` and `develop`. Fetches the build dependency, restores, runs `dotnet test`, builds the samples.
- `publish.yml`: on push to `main` and on `*.*.*` tags. Tag builds use the tag name with a leading `v` stripped. Main builds use GitVersion `fullSemVer` (`GitVersion.yml`: main = ContinuousDelivery, label `dev`). It then pushes the `.nupkg` to NuGet.org with Trusted Publishing (OIDC).
- `release-draft-action.yml`: on push to `main` or manual dispatch (optional `version` input). Drafts the next `vX.Y.Z` GitHub release with `HagiCode-org/hagi-release-drafter@v0.5.0`. Publishing the draft is a manual step, and its `vX.Y.Z` tag then triggers `publish.yml`. Tags before `v0.1.9` have no `v` prefix and are ignored by the action, so the first draft needs the `version` input (for example `0.1.9`).

## Conventions

- Package version: `Ducky.Sdk.nuspec` uses `$version`, injected by `packToLocal.sh`.
- Central package management in `Sdk/Directory.Packages.props`. MinVer is added only when `CI=true`.
- `Sdk/SDKlibs/Directory.Build.props` sets `netstandard2.1`, `ImplicitUsings=false`, `Nullable=enable`. Lib sets `LangVersion` 12. The other `Sdk/` projects (build runner, tests) target `net10.0` via `Sdk/Directory.Build.props`.
- `.editorconfig` files exist in `Sdk/` and `Samples/`.
- Because Lib is source-distributed and compiled into each mod, keep it free of dependencies the game does not provide. Keep public and internal API changes backward compatible (`InternalsVisibleTo Ducky.Sdk.Lib.Tests` is declared).
- Mod pattern: subclass `ModBehaviourBase` and implement `ModEnabled()` / `ModDisabled()`.
- Localization: a static `LK` class with `const string` keys (`[LanguageSupport("en","zh",...)]`, `[TranslateFile("md")]` for long text). The generator emits `L` accessors, `LKeys` and a key hash. CSVs live in the mod's `assets/Locales/`.
- Project properties read by the SDK (defaults in `Ducky.Sdk.props`): `ModName`, `IsModLib`, `ExcludeSdkLib`, `IncludeHarmony` (default false), `IncludeGameUi` (default false; references the game's `Plugins.dll` for rounded UI at compile time only), `EnableILRepack` (default true), `DeployMod` (default true, forced false for `IsModLib`), `EnableGlobalUsing` (default true), `DuckyDisablePromotion` (default false; `true` compiles out the startup line that names HagiCode), `DuckyHotReload`, `AssetsDir`, `LocalizationAssetsDir`, `SteamFolder` / `DuckovFolder` / `ManagedDirectory` / `ModsDirectory`. With `CI=true` and no folder set, `Managed/` and `artifacts/Mods/` under the git root are used.
- Hot reload is on by default for development builds (`DuckyHotReload`, set `false` to opt out). It is active only for a non-Release project that has a `ModName`, is not `IsModLib` and is deployed. See `docs/hot-reload.md` for what `ModDisabled()` must clean up.
- Localization shared code: `Sdk/Shared/Localization/LanguageCodes.cs`, `CsvDocument.cs` and `KeyPatternSyntax.cs` are linked (`<Compile Link>`) into Lib, Analyser and the build runner. Edit only the files in `Sdk/Shared/Localization/`. The build runner compiles its copy with `DUCKY_SDK_BUILD_SHARED` into its own namespace, so the tests can see both copies. Keep these files on netstandard2.0 APIs. Details: `docs/localization.md`.
- Commits and PRs: PR-number suffix style (for example "default directory for CI (#45)").

## Unity runtime constraints

Lib source is compiled into every mod and runs inside the game's Unity (Mono, `netstandard2.1`) process. Apply these rules when changing `Sdk/SDKlibs/Ducky.Sdk.Lib/`:

- **Main thread only for Unity APIs.** `GameObject.Find`, `new GameObject`, `AddComponent`, `DontDestroyOnLoad`, `Destroy`, `Application.*` and `GameplayDataSettings` access must run on the main thread. Do not call them inside `UniTask.RunOnThreadPool`, `Task.Run` or after a thread-pool continuation; use `await UniTask.SwitchToMainThread()` first. The ModHttpV1 hub (`ModHttpV1`, `ModHttpV1Proxy`, `MessageHubManager`) has been exercised in the real game; do not restructure its threading or lifetime without testing it in the game.
- **Static constructors must not touch Unity objects.** A failure there poisons the type with `TypeInitializationException` for the rest of the session. `BuffsContract.Instance` is a lazy property that finds or creates its holder `GameObject` on first use and re-creates it if Unity destroyed it. Keep new singletons in that shape.
- **Event subscriptions.** Handlers added through `ModBehaviourBase.Subscriptions` (`LevelEvents`, `SceneEvents`, or any `Action`) are released after `ModDisabled()`. A handler attached outside it still needs a matching `-=` in that teardown.
- **Static state outlives the disabled mod.** Mods can be enabled and disabled at runtime, and static state persists across that. `L.Detach()` unsubscribes `LocalizationManager.OnSetLanguage` when the last enabled `ModBehaviourBase` subclass in the mod is disabled (or its enable fails); `OnEnable` calls `L.Attach()` again. Deliberately left running: the MessageHub host and `ModHttpV1Proxy` (shared across mods) and `Log.Current` (re-assigned on every enable). Unity domain reload is not available at play time, so do not rely on it to reset statics.
- **Per-mod copies.** Because Lib is source-compiled, each mod has its own static `L`, `Log`, `ModOptions` and `Helper` caches. Cross-mod coordination happens only through scene objects found by name (the `ModHttpV1` hub, `<modId>_BuffsContractHolder`), reflection, or compiled `Expression` delegates. The hub's public method signatures are a cross-version contract: do not rename or change them.
- **Logging.** Inside Lib, use `Log` (LibLog). Use `Debug.Log*` only where the logger is not yet initialized (`ModBehaviourBase` startup, `Helper`, `LocalizationService`). Consumer mods get `DUCKY001`-`DUCKY005` warnings for `Debug.Log*`.
- **Paths and IO.** `Assembly.Location` can be empty and `Path.GetDirectoryName` can return null (see `LocalizationService`, `Helper.GetModId`); keep a fallback. Config goes to `Application.persistentDataPath` (`ModOptions`); mod assets sit next to the DLL. Keep file IO off per-frame paths and cache parsed results.
- **Reflection and stripping.** The game runs on Mono, so `Expression.Compile` and reflection work. If the game moves to IL2CPP, `ModHttpV1Proxy` (`Expression.Lambda` over a reflected hub type) and `Helper.GetModName` (type scan) must be revisited. Avoid adding new runtime code generation.
- **Unity serialization and ILRepack.** ILRepack runs with `/internalize`. Types that Unity serializes or finds by name (`MonoBehaviour` subclasses, `ModHttpV1`) must keep their names. `MessageHubManager` matches `GetType().Name == "ModHttpV1"`. Do not rename them.
- **Game updates.** `GameApis/` and `CodeGenTests.cs` depend on game types and field names. A game update can break them silently at runtime (reflection) or at compile time. After refreshing `Managed/`, rebuild Lib, run tests and `rebuild_samples.sh`.

## Gotchas

- `Local.props` is git-ignored (`Sdk/.gitignore`), but `Sdk/Docky.Sdk.slnx` lists it anyway. It is imported by `Sdk/Directory.Build.props`, `Ducky.Sdk.Lib.csproj` and the samples. Define `<SteamFolder>/path/to/steam/</SteamFolder>` there. `Sdk/Directory.Build.props` fails the build early without `SteamFolder`, `DuckovFolder`, `ManagedDirectory` or `ModsDirectory`, unless `CI=true`.
- `nuget.props` is committed and rewritten on every auto-versioned pack. The sample csproj files hard-code `Ducky.Sdk` versions (currently several different `0.0.2xxxxx-dev` values), and `rebuild_samples.sh` rewrites them. Expect noisy diffs in `nuget.props` and `Samples/**/*.csproj` after local runs. Do not commit them accidentally.
- An auto-incremented version can collide with a stale extraction in `~/.nuget/packages/ducky.sdk/<version>`. NuGet then silently builds the samples against the OLD package. Symptom: sample errors that do not match the current targets or scripts. Before trusting `rebuild_samples.sh`, bump `nuget.props` to an unused version (for example `0.0.200000-dev`) or pass `--purge-all-versions`.
- `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.nuspec` is tracked even though `.gitignore` lists `Sdk/SDKlibs/Ducky.Sdk/*.nuspec`. `packToLocal.sh` creates temp `Ducky.Sdk.<version>.nuspec` files next to it. Never commit those.
- The nuspec packs `Sdk/Ducky.Sdk.Analyser/bin/netstandard2.0/Ducky.Sdk.Analyser.dll`, so the Analyser must be built first (the pack script does this). The packed files are the Lib `.cs` files, the runner under `tools/net10.0/` (without `.pdb`), `Ducky.Sdk.props`, the `.targets` files and `0Harmony.dll`. A new file outside those globs will not ship.
- `Ducky.Sdk.Validation.targets` is imported first by `Ducky.Sdk.targets`. It fails the build early for project paths with command-line special characters, and when none of `DuckovFolder`, `SteamFolder`, `ManagedDirectory` or `ModsDirectory` resolves. Keep it in the nuspec if you rename it.
- Build steps run in the bundled runner (`Ducky.Sdk.Orchestration.targets`). `GenerateBuildContext` writes `obj/ducky-build-properties.txt`. The pre phase (`--phase pre`, after `CoreCompile`) writes `obj/ducky-build-context.json` and runs the pre steps. The post phase (`--phase post`, after `Build`) runs the post steps. The runner runs on `$(DOTNET_HOST_PATH)` (fallback `dotnet`). Deploy deletes and recreates the mod folder under the game's `Duckov_Data/Mods/`.
- Build speed: a build starts two runner processes (one per phase). The step order is the one in `Ducky.Sdk.Build/Runner/BuildRunner.cs`.
- Runner errors: a missing or too-old .NET runtime makes the build fail with a message that names the .NET 10 runtime and its download page. A missing runner DLL names the package to restore.
- Do not commit MSBuild binary logs (`*.binlog` is git-ignored; `build.binlog` was once committed by mistake).
- Sample mods publish to `Samples/*/Publish` (git-ignored) when `ModsDirectory` is set to `./Publish`.
- `ModBehaviourBase.OnDisable` still rethrows `ModDisabled()` exceptions after cleanup (Unity logs them). Cleanup itself never throws.
- Several Lib files are vendored third-party sources (LibLog providers, `Contracts/System.CommandLine/`). Avoid reformatting them.

## Testing status

- Covered by `Sdk/Tests/Ducky.Sdk.Lib.Tests`: `ModOptions`; the build steps and runner (`BuildRunner/`, including the golden build context); the localization runtime, service, CSV parsing, key patterns and generator (`LocalizationGeneratorTests`); `MainThreadQueue`; `ModSubscriptions` and the disable ordering (`ModBehaviourBase.RunDisableSequence`, which still releases and rethrows when `ModDisabled()` throws); `EventSubscription`; `ModId`, `ModInfo` and mod folder resolution (`Helper.ResolveModFolderPath`, `Helper.ResolveModId`); `UnityThreadingAnalyzer` (DUCKY006 and DUCKY007); hot reload; debug events; the HTTP endpoint protocol, client and host core; and pure game-rule helpers.
- Not covered by any test: the `DUCKY001`-`DUCKY005` analyzers (no test references `UnityDebugAnalyzer`); `MessageHubManager` and `ModHttpV1`; `ModBehaviourBase.OnEnable` and `OnDisable` as a whole (only the disable ordering is tested); the `MainThread` facade and `MainThreadPump` (they create a Unity `GameObject`); `LevelEvents` and `SceneEvents` (they need real game and Unity events); the production constructor and override calls of `LocalizationService`; the `BuffsContract` holder and buff registration; `ItemSetting_GunExtensions` on the real game type. These need the game to exercise.
- `packToLocal.sh` only warns when tests fail, so a local pack can ship with red tests. CI does fail on test errors. Run `dotnet test` yourself for a reliable result.

## Related repositories

- `ducky7go/Ducky.Samples` - further sample mods (BuffRegistration, InstantKill, Localization, ...) that consume the published `Ducky.Sdk` package from NuGet (currently 0.1.6).
- `ducky7go/sdk_build_dependency` - release of the game `Managed` assemblies, fetched by CI and `fetch_build_dependency.sh`.
- `ducky7go/Ducky.Terminals` - the terminal UI mod that `ModTerminalClientContract` targets (`TerminalUIMod`).
- `Newbe.BetterModLoader` - the legacy BetterModLoader integration is `Logging/LogProviders/BmlSerilogLogProvider.cs`.
