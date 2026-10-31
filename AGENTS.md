<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# Ducky.Sdk - AI Coding Agent Instructions

## Project overview

Ducky.Sdk is the NuGet package `Ducky.Sdk` (MIT, author Newbe36524, repo `ducky7go/Ducky.Sdk`) for building mods for the Unity game "Escape from Duckov" in C#. Mod authors add a single `PackageReference` and get: shared library source compiled into their mod, a Roslyn localization generator and a Unity-logging analyzer, an MSBuild pipeline that deploys the mod to the game folder, auto-generated `info.ini` / `preview.png`, ILRepack merging into one DLL, and optional Harmony (`0Harmony.dll`). It is a `developmentDependency`; the library is distributed as **source** (contentFiles), not as a compiled assembly. Docs: `README.md` (Chinese), `README_en.md` (English). Specs and change history live in `openspec/` (`specs/` has `continuous-integration`, `mod-build`, `mod-options`; `changes/archive/` holds past changes).

## Repository layout

- `Sdk/Docky.Sdk.slnx` - solution (note the `Docky` spelling): Analyser, Lib, package project, tests.
- `Sdk/Ducky.Sdk.Analyser/` - `netstandard2.0` Roslyn project. `DuckyLocalizationGenerator.cs` (incremental generator for the `LK` -> `L` localization pattern), `UnityDebugAnalyzer.cs` (diagnostics `DUCKY001`-`DUCKY005` for `Debug.Log*` calls) and `UnityDebugCodeFixProvider.cs`.
- `Sdk/SDKlibs/Ducky.Sdk.Lib/` - `netstandard2.1` source library (`ModBehaviourBase`, `L` / `LocalizationService` / `CsvParser`, `Logging/` (LibLog-style), `Options/ModOptions`, `Utils/`, `Attributes/`, `Contracts/` (ModHttpV1 message hub, terminal client contract, vendored System.CommandLine sources, buffs), `GameApis/` (extension methods over game types)). Needs the game's `Managed` DLLs to compile.
- `Sdk/SDKlibs/Ducky.Sdk/` - the package project: `Ducky.Sdk.csproj` (packs via `Ducky.Sdk.nuspec`), `Ducky.Sdk.props`, `Ducky.Sdk.targets` (only imports `Ducky.Sdk.Orchestration.targets`), `0Harmony.dll`.
- `Sdk/SDKlibs/scripts/` - dotnet-script build logic: `entry.csx`, `ContextJsonBuild.csx`, `libs/*Lib.cs` (one per build step: localization extraction/CSV update, info.ini, preview, deploy, ILRepack, dependency copy, ...), `shared/BuildContext.cs`. Packed into the NuGet under `scripts/`.
- `Sdk/Tests/Ducky.Sdk.Lib.Tests/` - NUnit + Shouldly tests (`CodeGenTests.cs`, `ModOptionsTests.cs`).
- `Samples/` - sample mods (`Docky.Sdk.Sample.slnx`): `Ducky.SingleProject`, `Ducky.EntranceMod` + `Ducky.EntranceMod2` + `Ducky.EntranceMod.Common` (multi-project, `IsModLib`), `Ducky.TryHarmony`, `Ducky.MessageHubHost/Client/UI`. They are SDK integration tests, not templates (the `Ducky.Samples` repo holds further samples that consume the published package).
- `scripts/` - `packToLocal.sh`, `rebuild_samples.sh`, `fetch_build_dependency.sh`.
- `duckylocal/` - local NuGet feed (git-ignored except `placeholder.txt`); `nuget.props` - holds `LocalNuGetVersion`; `GitVersion.yml`; `.config/dotnet-tools.json` (dotnet-script 1.6.0, JetBrains ReSharper `jb` global tool 2025.1.4).

## Build / test / run

Prerequisites: .NET SDK 9 (root `global.json` pins 9.0.302, `rollForward: latestMajor`; CI uses this file), `dotnet tool restore` (dotnet-script), and `dotnet-ilrepack` (CI installs `--version 2.0.44` globally). The game's `Managed` assemblies are not in git: `scripts/fetch_build_dependency.sh` downloads the latest release of `ducky7go/sdk_build_dependency` into `./Managed/` (git-ignored; the Lib and test projects prefer it over a game install). Alternatively set `SteamFolder` (see Gotchas).

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
- `nuget.props` is committed and rewritten on every auto-versioned pack; the sample csproj files hard-code `Ducky.Sdk` `0.0.100191-dev`, which `rebuild_samples.sh` rewrites. Expect noisy diffs in `nuget.props` and `Samples/**/*.csproj` after local runs; do not commit them accidentally.
- `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.nuspec` is tracked even though `.gitignore` lists `Sdk/SDKlibs/Ducky.Sdk/*.nuspec`; `packToLocal.sh` creates temp `Ducky.Sdk.<version>.nuspec` files next to it - never commit those.
- The nuspec packs `Sdk/Ducky.Sdk.Analyser/bin/netstandard2.0/Ducky.Sdk.Analyser.dll`, so the Analyser must be built first (the pack script does this). Lib `.cs` files, `scripts/**/*.csx` and `scripts/**/*.cs`, `Ducky.Sdk.props`, `.targets` and `0Harmony.dll` are packed; a new file outside those globs will not ship.
- `Ducky.Sdk.Validation.targets` is neither imported by `Ducky.Sdk.targets` nor listed in the nuspec, so it has no effect on consumers.
- Build steps run through `dotnet script` (`Ducky.Sdk.Orchestration.targets` -> `GenerateBuildContext` writes `obj/ducky-build-context.json`, then `entry.csx` runs the `*Lib` steps in `ScriptLibraries` order). `dotnet-script` must be on PATH or consumer builds fail. Deploy deletes and recreates the mod folder under the game's `Duckov_Data/Mods/`.
- `build.binlog` was a stray committed artifact; it is now untracked and `*.binlog` is git-ignored. Do not commit MSBuild binary logs.
- Sample mods publish to `Samples/*/Publish` (git-ignored) when `ModsDirectory` is set to `./Publish`.
- Several Lib files are vendored third-party sources (LibLog providers, `Contracts/System.CommandLine/`); avoid reformatting them.

## Related repositories

- `ducky7go/Ducky.Samples` - further sample mods (BuffRegistration, InstantKill, Localization, ...) that consume the published `Ducky.Sdk` package from NuGet (currently 0.1.6).
- `ducky7go/sdk_build_dependency` - release of the game `Managed` assemblies fetched by CI and `fetch_build_dependency.sh`.
- `Lib/Contracts/ModTerminalClientContract` targets the `TerminalUIMod` mod id (terminal UI mod, see `Ducky.Terminals` repo); `ModHttpV1` is the SDK's built-in inter-mod message hub.
- `Logging/LogProviders/BmlSerilogLogProvider.cs` looks for the `Newbe.BetterModLoader` assembly (legacy BetterModLoader integration).
