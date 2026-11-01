## OpenSpec

This repo has no `openspec/` directory. OpenSpec for all `ducky7go-mono` sub-repos lives in the parent repo: `../../openspec/` (workflow rules `../../openspec/AGENTS.md`, specs `../../openspec/specs/`, changes `../../openspec/changes/`). This repo's former project context is `../../openspec/projects/Ducky.Sdk/project.md`.


# Ducky.Sdk - AI Coding Agent Instructions

## Project overview

Ducky.Sdk is the NuGet package `Ducky.Sdk` (MIT, author Newbe36524, repo `ducky7go/Ducky.Sdk`) for building mods for the Unity game "Escape from Duckov" in C#. Mod authors add a single `PackageReference` and get: shared library source compiled into their mod, a Roslyn localization generator and a Unity-logging analyzer, an MSBuild pipeline that deploys the mod to the game folder, auto-generated `info.ini` / `preview.png`, ILRepack merging into one DLL, and optional Harmony (`0Harmony.dll`). It is a `developmentDependency`; the library is distributed as **source** (contentFiles), not as a compiled assembly. Docs: `README.md` (Chinese), `README_en.md` (English). Specs and change history live in `../../openspec/` (`specs/` has `continuous-integration`, `mod-build`, `mod-options`; `changes/archive/` holds past changes).

## Repository layout

- `Sdk/Docky.Sdk.slnx` - solution (note the `Docky` spelling): Analyser, Lib, package project, tests.
- `Sdk/Ducky.Sdk.Analyser/` - `netstandard2.0` Roslyn project. `DuckyLocalizationGenerator.cs` (incremental generator for the `LK` -> `L` localization pattern), `UnityDebugAnalyzer.cs` (diagnostics `DUCKY001`-`DUCKY005` for `Debug.Log*` calls) and `UnityDebugCodeFixProvider.cs`.
- `Sdk/SDKlibs/Ducky.Sdk.Lib/` - `netstandard2.1` source library (`ModBehaviourBase`, `L` / `LocalizationService` / `CsvParser`, `Logging/` (LibLog-style), `Options/ModOptions`, `Utils/`, `Attributes/`, `Contracts/` (ModHttpV1 message hub, terminal client contract, vendored System.CommandLine sources, buffs), `GameApis/` (extension methods over game types)). Needs the game's `Managed` DLLs to compile.
- `Sdk/SDKlibs/Ducky.Sdk/` - the package project: `Ducky.Sdk.csproj` (packs via `Ducky.Sdk.nuspec`), `Ducky.Sdk.props`, `Ducky.Sdk.targets` (only imports `Ducky.Sdk.Orchestration.targets`), `0Harmony.dll`.
- `Sdk/SDKlibs/scripts/` - dotnet-script build logic: `entry.csx`, `ContextJsonBuild.csx`, `libs/*Lib.cs` (one per build step: localization extraction/CSV update, info.ini, preview, deploy, ILRepack, dependency copy, ...), `shared/BuildContext.cs`. Packed into the NuGet under `scripts/`.
- `Sdk/Tests/Ducky.Sdk.Lib.Tests/` - NUnit + Shouldly tests (`CodeGenTests.cs`, `ModOptionsTests.cs`, `BuildScriptsTests.cs`). The csproj links `SDKlibs/scripts/shared/BuildContext.cs` and `libs/ILRepackAssembliesLib.cs` into the test assembly; `BuildScriptGlobals.cs` stands in for csx script-level globals (`SkipExitCode`) those files reference unqualified.
- `Samples/` - sample mods (`Docky.Sdk.Sample.slnx`): `Ducky.SingleProject`, `Ducky.EntranceMod` + `Ducky.EntranceMod2` + `Ducky.EntranceMod.Common` (multi-project, `IsModLib`), `Ducky.TryHarmony`, `Ducky.MessageHubHost/Client/UI`. They are SDK integration tests, not templates (the `Ducky.Samples` repo holds further samples that consume the published package).
- `scripts/` - `packToLocal.sh`, `rebuild_samples.sh`, `fetch_build_dependency.sh`.
- `duckylocal/` - local NuGet feed (git-ignored except `placeholder.txt`); `nuget.props` - holds `LocalNuGetVersion`; `GitVersion.yml`; `.config/dotnet-tools.json` (dotnet-script 2.0.1, JetBrains ReSharper `jb` global tool 2026.2.4).

## Build / test / run

Prerequisites: .NET SDK 10 (root `global.json` pins 10.0.402, `rollForward: latestFeature`; CI uses this file), `dotnet tool restore` (dotnet-script), and `dotnet-ilrepack` (CI installs `--version 2.0.48` globally). Lib targets `netstandard2.1` because the game runs on Unity's Mono runtime; the .NET 10 SDK is only the build toolchain, not the runtime (see "Unity runtime constraints"). The game's `Managed` assemblies are not in git: `scripts/fetch_build_dependency.sh` downloads the latest release of `ducky7go/sdk_build_dependency` into `./Managed/` (git-ignored; the Lib and test projects prefer it over a game install). Alternatively set `SteamFolder` (see Gotchas).

- Unit tests (as CI): `dotnet test Sdk/Docky.Sdk.slnx --configuration Release`
- Pack to local feed: `./scripts/packToLocal.sh [--version X] [--configuration Debug|Release] [--skip-tests] [--no-build] [--purge-all-versions] [--no-clear-cache] [--max-packages N] [--no-cleanup]`. It builds the Analyser, builds the package project, runs tests (failures only warn), then `dotnet pack` with a temp nuspec into `duckylocal/`. Without `--version` it auto-increments `nuget.props` (`0.0.XXXXXX-dev`).
- End-to-end check: `./scripts/rebuild_samples.sh` (runs `dotnet tool restore`, packs, rewrites the `Ducky.Sdk` version in every sample csproj via `dotnet add package`, then builds `Samples/Docky.Sdk.Sample.slnx`). This is what CI runs after the tests.
- `bash scripts/fetch_build_dependency.sh` needs network; do not run it casually.

## Conventions

- Package version: `Ducky.Sdk.nuspec` uses `$version`; the version is injected by `packToLocal.sh`. CI (`publish.yml`, on push to `main` and on `*.*.*` tags) uses the tag name for tags, or GitVersion `fullSemVer` (`GitVersion.yml`: main = ContinuousDelivery, label `dev`) for main, then pushes `.nupkg` to MyGet (continue-on-error) and NuGet.org.
- PR workflow: `build_dotnet.yml` (restore, test, rebuild samples; on `main`/`develop`), `dotnet_format.yml` (ReSharper `jb cleanupcode` on changed files, auto-commits to the PR branch), `release-drafter.yml` (labels such as "Bug Fix", "Feature" in `.github/release-drafter.yml`).
- Central package management in `Sdk/Directory.Packages.props` (MinVer is added only when `CI=true`). `Sdk/SDKlibs/Directory.Build.props` sets `netstandard2.1`, `ImplicitUsings=false`, `Nullable=enable`; Lib uses `LangVersion` 12. `.editorconfig` files exist in `Sdk/` and `Samples/`.
- Because Lib is source-distributed and compiled into each mod, keep it free of dependencies the game does not provide and keep public/internal API changes backward compatible (`InternalsVisibleTo Ducky.Sdk.Lib.Tests` is declared).
- Mod pattern: subclass `ModBehaviourBase`, implement `ModEnabled()` / `ModDisabled()`. Localization: a static `LK` class with `const string` keys (`[LanguageSupport("en","zh",...)]`, `[TranslateFile("md")]` for long text) -> generator emits `L` accessors, `LKeys`, and a key hash; CSVs live in the mod's `assets/Locales/`.
- Project properties read by the SDK: `ModName`, `IsModLib`, `ExcludeSdkLib`, `IncludeHarmony` (default false), `EnableILRepack` (default true), `DeployMod` (default true, forced false for `IsModLib`), `EnableGlobalUsing` (default true), `AssetsDir`, `LocalizationAssetsDir`, `SteamFolder` / `DuckovFolder` / `ManagedDirectory` / `ModsDirectory`. Defaults are in `Ducky.Sdk.props`; with `CI=true` and no folder set, `Managed/` and `artifacts/Mods/` under the git root are used.
- Commits/PRs: this checkout is a shallow clone with one visible commit, "default directory for CI (#45)", i.e. PR-number suffix style; OpenSpec change proposals are the planning mechanism.

## Gotchas

- `Local.props` (git-ignored) is imported by `Sdk/Directory.Build.props`, `Ducky.Sdk.Lib.csproj` and the samples; define `<SteamFolder>/path/to/steam/</SteamFolder>` there. `Sdk/Directory.Build.props` fails the build early without `SteamFolder`/`DuckovFolder`/`ManagedDirectory`/`ModsDirectory` unless `CI=true`; `Docky.Sdk.slnx` lists `Local.props` even though it is not committed.
- `nuget.props` is committed and rewritten on every auto-versioned pack; the sample csproj files hard-code `Ducky.Sdk` `0.0.100191-dev`, which `rebuild_samples.sh` rewrites. Expect noisy diffs in `nuget.props` and `Samples/**/*.csproj` after local runs; do not commit them accidentally. The auto-incremented version can collide with a stale extraction in `~/.nuget/packages/ducky.sdk/<version>` from an earlier run; NuGet then silently builds the samples against the OLD package (symptom: sample errors that do not match the current targets/scripts). Before trusting `rebuild_samples.sh`, bump `nuget.props` to an unused version (for example `0.0.200000-dev`) or pass `--purge-all-versions`.
- `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.nuspec` is tracked even though `.gitignore` lists `Sdk/SDKlibs/Ducky.Sdk/*.nuspec`; `packToLocal.sh` creates temp `Ducky.Sdk.<version>.nuspec` files next to it - never commit those.
- The nuspec packs `Sdk/Ducky.Sdk.Analyser/bin/netstandard2.0/Ducky.Sdk.Analyser.dll`, so the Analyser must be built first (the pack script does this). Lib `.cs` files, `scripts/**/*.csx` and `scripts/**/*.cs`, `Ducky.Sdk.props`, `.targets` and `0Harmony.dll` are packed; a new file outside those globs will not ship.
- `Ducky.Sdk.Validation.targets` is imported first by `Ducky.Sdk.targets` and packed into `build/`. It fails the build early for project paths with command-line special characters, and when none of `DuckovFolder` / `SteamFolder` / `ManagedDirectory` / `ModsDirectory` can be resolved (it depends on `InitializeDuckySdkProperties`, so `CI=true` defaults count). Keep it in the nuspec if you rename it.
- Build steps run through `dotnet script` (`Ducky.Sdk.Orchestration.targets` -> `GenerateBuildContext` writes `obj/ducky-build-context.json`, then `entry.csx` runs the `*Lib` steps in `ScriptLibraries` order). `dotnet-script` must be on PATH or consumer builds fail. Deploy deletes and recreates the mod folder under the game's `Duckov_Data/Mods/`.
- `build.binlog` was a stray committed artifact; it is now untracked and `*.binlog` is git-ignored. Do not commit MSBuild binary logs.
- Sample mods publish to `Samples/*/Publish` (git-ignored) when `ModsDirectory` is set to `./Publish`.
- Several Lib files are vendored third-party sources (LibLog providers, `Contracts/System.CommandLine/`); avoid reformatting them.

## Unity runtime constraints

Lib source is compiled into every mod and runs inside the game's Unity (Mono, `netstandard2.1`) process. Apply these rules when changing `Sdk/SDKlibs/Ducky.Sdk.Lib/`:

- **Main thread only for Unity APIs.** `GameObject.Find`, `new GameObject`, `AddComponent`, `DontDestroyOnLoad`, `Destroy`, `Application.*`, and `GameplayDataSettings` access must run on the main thread. Do not call them inside `UniTask.RunOnThreadPool`, `Task.Run` or after a thread-pool continuation; use `await UniTask.SwitchToMainThread()` first. The ModHttpV1 hub (`ModHttpV1`, `ModHttpV1Proxy`, `MessageHubManager`) has been exercised in the real game and works; do not restructure its threading or lifetime without testing it in the game.
- **Static constructors must not touch Unity objects.** A failure in a static constructor (for example a first access off the main thread) poisons the type with `TypeInitializationException` for the rest of the session. `BuffsContract.Instance` is therefore a lazy property that finds or creates its holder `GameObject` on first use and re-creates it if Unity destroyed it; keep new singletons in that shape.
- **Static state outlives the mod being disabled.** Mods can be enabled/disabled at runtime and static state persists across that. `ModBehaviourBase` now undoes what enabling attached: `L.Detach()` unsubscribes `LocalizationManager.OnSetLanguage` when the last enabled `ModBehaviourBase` subclass in the mod is disabled (or its enable fails), and `OnEnable` calls `L.Attach()` again. Do not add new event subscriptions without a matching `-=` in that teardown. Deliberately left running: the MessageHub host and `ModHttpV1Proxy` (shared across mods, tested in the game) and `Log.Current` (re-assigned on every enable). Unity domain reload is not available at play time, so do not rely on it to reset statics.
- **Per-mod copies.** Because Lib is source-compiled, each mod has its own static `L`, `Log`, `ModOptions` and `Helper` caches. Cross-mod coordination only happens through scene objects found by name (`ModHttpV1` hub, `<modId>_BuffsContractHolder`) and reflection or compiled `Expression` delegates, so the hub's public method signatures are a cross-version contract: do not rename or change them.
- **Logging.** Inside Lib, use `Log` (LibLog) for diagnostic output and `Debug.Log*` only where the logger is not yet initialized (`ModBehaviourBase` startup, `Helper`, `LocalizationService`). Consumer mods get `DUCKY001`-`DUCKY005` warnings for `Debug.Log*`.
- **Paths and IO.** `Assembly.Location` can be empty and `Path.GetDirectoryName` can return null (see `LocalizationService`, `Helper.GetModId`); always keep a fallback. Config goes to `Application.persistentDataPath` (`ModOptions`), mod assets next to the DLL in the mod folder. Keep file IO off per-frame paths and cache parsed results.
- **Reflection / stripping.** The game currently runs on Mono, so `Expression.Compile` and reflection work. If the game ever moves to IL2CPP, `ModHttpV1Proxy` (`Expression.Lambda` over a reflected hub type) and `Helper.GetModName` (type scan) must be revisited; avoid adding new runtime code generation.
- **Unity serialization and ILRepack.** ILRepack runs with `/internalize`; types that Unity serializes or finds by name (`MonoBehaviour` subclasses, `ModHttpV1`) must keep their names, and `MessageHubManager` matches `GetType().Name == "ModHttpV1"`. Do not rename them.
- **Game updates.** `GameApis/` and `CodeGenTests.cs` depend on game types and field names; a game update can break them silently at runtime (reflection) or at compile time. After refreshing `Managed/`, rebuild Lib, run tests and `rebuild_samples.sh`.

## Testing status and known gaps

What is covered and what is still open.

- Covered by `Sdk/Tests/Ducky.Sdk.Lib.Tests`: `ModOptions` (storage faked via `IModOptionsStorage`) and, via linked script sources, `BuildUtils.ExecuteCommand` (arguments with spaces, large stderr without deadlock, missing tool message; the process tests are Linux/macOS only) and `ILRepackAssembliesLib.BuildILRepackArgs` (paths with spaces stay single unquoted arguments). `CodeGenTests.cs` is a generator-style helper, not an assertion test. Run `dotnet test Sdk/Docky.Sdk.slnx --configuration Release`.
- Not covered by any test: `Ducky.Sdk.Analyser` (`DUCKY001`-`DUCKY005`, `DuckyLocalizationGenerator`), the other `SDKlibs/scripts/` build steps, `LocalizationService` / `CsvParser`, `MessageHubManager` / `ModHttpV1`, `ModBehaviourBase`, `BuffsContract`. Unity-bound code cannot run outside the game (UnityEngine calls into native code), so it needs a seam (interface or injectable delegate) before it can be unit tested, as `ModOptions` does with `IModOptionsStorage`.
- End-to-end evidence: `CI=true bash scripts/rebuild_samples.sh` (with an unused package version, see Gotchas) packs, builds all samples and runs ILRepack for the mod projects. Revert `nuget.props` and `Samples/**/*.csproj` afterwards.
- Build speed: a build runs about 14 separate `dotnet script` processes (`Ducky.Sdk.Orchestration.targets`) with no `Inputs`/`Outputs` incremental checks. The execution-order comment at the top of that file does not match the actual order (post-build steps run `ResetDeployLib`, `ILRepackAssembliesLib`, `CopyDependenciesLib`, `DeployModLib`).
- Tooling errors: a missing `dotnet-script` now ends in "Failed to serialize BuildContext JSON ... run `dotnet tool restore`" and a missing `ilrepack` in "Failed to start 'ilrepack' ... dotnet tool install --global dotnet-ilrepack". A `dotnet script` path that contains `..` (for example `build/../scripts/x.csx`) makes dotnet-script fail with "Could not load file or assembly ... script.dll"; always pass normalized paths (`$([System.IO.Path]::GetFullPath(...))`) to it.
- `packToLocal.sh` only warns when tests fail, so a local pack can ship with red tests; CI does fail on test errors. Its internal test step can also run stale binaries (it reported the old test count after tests were added), so run `dotnet test` yourself.
- `ModBehaviourBase.OnDisable` still rethrows `ModDisabled()` exceptions after cleaning up (Unity logs them); cleanup itself never throws.

## Related repositories

- `ducky7go/Ducky.Samples` - further sample mods (BuffRegistration, InstantKill, Localization, ...) that consume the published `Ducky.Sdk` package from NuGet (currently 0.1.6).
- `ducky7go/sdk_build_dependency` - release of the game `Managed` assemblies fetched by CI and `fetch_build_dependency.sh`.
- `Lib/Contracts/ModTerminalClientContract` targets the `TerminalUIMod` mod id (terminal UI mod, see `Ducky.Terminals` repo); `ModHttpV1` is the SDK's built-in inter-mod message hub.
- `Logging/LogProviders/BmlSerilogLogProvider.cs` looks for the `Newbe.BetterModLoader` assembly (legacy BetterModLoader integration).
