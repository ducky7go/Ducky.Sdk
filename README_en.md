# Ducky.Sdk

**[中文](README.md) | English**

A comprehensive .NET SDK for developing mods for "Escape from Duckov" game.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download)
[![NuGet](https://img.shields.io/nuget/v/Ducky.Sdk.svg)](https://www.nuget.org/packages/Ducky.Sdk/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Ducky.Sdk.svg)](https://www.nuget.org/packages/Ducky.Sdk/)
[![GitHub Stars](https://img.shields.io/github/stars/ducky7go/Ducky.Sdk?style=social)](https://github.com/ducky7go/Ducky.Sdk/stargazers)
[![GitHub Issues](https://img.shields.io/github/issues/ducky7go/Ducky.Sdk)](https://github.com/ducky7go/Ducky.Sdk/issues)
[![GitHub Pull Requests](https://img.shields.io/github/issues-pr/ducky7go/Ducky.Sdk)](https://github.com/ducky7go/Ducky.Sdk/pulls)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/ducky7go/Ducky.Sdk/pulls)

## Features

- 🚀 **Automated Build Pipeline** - Build-time deployment to game directories
- 🌍 **Intelligent Localization** - Source generator-based localization with CSV/file-based translations
- 📦 **Single-DLL Distribution** - Automatic assembly merging via ILRepack for conflict-free deployment
- 🔧 **Harmony Integration** - Optional runtime patching support with seamless dependency management
- 🎨 **Auto-Generated Assets** - Automatic mod metadata and preview image generation
- 📝 **Strongly-Typed Development** - Full IntelliSense support with compile-time validation
- 🔄 **Source-Code Distribution** - SDK distributed as source code to avoid version conflicts

## Quick Start

### Prerequisites

- .NET SDK 10.0 or later (the build runner needs the .NET 10 runtime that ships with the SDK; no `dotnet-script` or `ilrepack` install is needed)
- "Escape from Duckov" game installed
- Steam installation directory (for automated deployment)

### Installation

1. **Create `Local.props` in your project root** (git-ignored):

```xml
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/your/steam/</SteamFolder>
  </PropertyGroup>
</Project>
```

2. **Configure your mod project:**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <ModName>MyAwesomeMod</ModName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Ducky.Sdk" Version="*">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### CI/CD Environment Configuration

In continuous integration environments (GitHub Actions, Azure Pipelines, etc.), you can build mods without Steam installed by specifying key paths via environment variables:

```yaml
# GitHub Actions example
env:
  ManagedDirectory: /opt/game-assemblies/Managed/
  ModsDirectory: /tmp/mod-output/
```

Or pass via MSBuild parameters:

```bash
dotnet build -p:ManagedDirectory=/path/to/managed/ -p:ModsDirectory=/path/to/mods/
```

**CI Build Benefits:**
- No Steam installation required
- No game files needed
- Flexible output path configuration
- Automatic `SteamFolder` validation bypass (detects `CI` environment variable)

### Your First Mod

Create a `ModBehaviour.cs` file:

```csharp
using Ducky.Sdk.Logging;
using Ducky.Sdk.ModBehaviours;

namespace MyAwesomeMod;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        Log.Info("My Awesome Mod Enabled!");
        // Your initialization code here
    }

    protected override void ModDisabled()
    {
        Log.Info("My Awesome Mod Disabled!");
        // Cleanup code here
    }
}
```

Build and run:

```bash
dotnet build
# Your mod is automatically deployed to the game directory!
```

## Core Concepts

### ModBehaviour Pattern

Every mod extends `ModBehaviourBase` which provides:

- **Lifecycle Hooks**: `ModEnabled()` and `ModDisabled()`
- **Automatic Initialization**: Logging, localization, and buff registration
- **Mod Identity**: Access to mod name, ID, and Steam Workshop metadata

```csharp
public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        // Called when mod is loaded
        // Initialize systems, register events, apply patches
    }

    protected override void ModDisabled()
    {
        // Called when mod is unloaded
        // Cleanup resources, unpatch methods, unregister events
    }
}
```

### Localization System

The SDK uses a unique two-class pattern for type-safe localization:

#### 1. Define Keys (LK.cs)

```csharp
using Ducky.Sdk.Attributes;

[LanguageSupport("en", "zh", "fr")]
public static class LK
{
    public static class UI
    {
        public const string Welcome = "Welcome to my mod!";
        public const string Settings = "Settings";
        
        [TranslateFile("md")]
        public const string Documentation = "Documentation";
    }
}
```

#### 2. Use Generated Properties (Auto-generated L.cs)

```csharp
using Ducky.Sdk.Localizations;

// The SDK generates a matching L class with properties
Log.Info(L.UI.Welcome);        // Returns localized string
Log.Info(L.UI.Documentation);  // Returns content from Documentation.md
```

#### 3. Manage Translations

Translations are stored in `assets/Locales/`:

```
assets/
  Locales/
    en.csv         # English translations
    zh.csv         # Chinese translations
    en/
      Documentation.md  # Long-form English content
    zh/
      Documentation.md  # Long-form Chinese content
```

CSV format:

```csv
Key,Value
Welcome to my mod!,Welcome to my mod!
Settings,Settings
Documentation,Documentation.md
```

#### 4. Runtime API, file markers, fallback and validation

See the [localization guide](docs/localization.md) for the full rules, and the [DOD migration guide](docs/localization-dod-migration.md) for moving a mod off its own localization code.

- Runtime: `L.Get`, `L.TryGet`, `L.Dynamic` (formats a key template such as `"worldeffect.{0}.name"`). `L.RegisterLocales(modId, dir)` registers a sub-mod's `Locales` folder, and `L.Scope(modId)` looks up only that mod's sources.
- Data-driven keys: declare `[KeyPattern("worldeffect.{code}.{name|summary|description}", Group = "WorldEffect")]` on `LK` to get typed accessors such as `L.WorldEffect.Name(code)`.
- File references: a CSV value is a file only when it starts with `@file:` (for example `@file:Welcome.md`). Any other value is text.
- Language codes are lower-case with `-` (`zh-Hant` becomes `zh-hant`). The default fallback chain is `zh-hant -> zh -> en`, and `L.ConfigureFallback` changes it.
- Keys ignore case. Missing text gives `[key]`; `L.TryGet` reports the absence without a placeholder.
- Build validation compares every language with the English baseline. `LocalizationValidation` (`Off` / `Warn` / `Error`, default `Warn`) controls missing keys and placeholder mismatches; `LocalizationExtraKeys` (`Ignore` / `Warn` / `Error`, default `Warn`) controls extra keys. Missing translations are written to `missing.csv`.
- Translation commands: `dotnet tools/net10.0/Ducky.Sdk.Build.dll locales set | sync | validate ...`.

### Configuration Management

Store and retrieve mod settings:

```csharp
using Ducky.Sdk.Options;

// Per-mod configuration (isolated storage)
ModOptions.ForThis.Set("volume", 0.8);
var volume = ModOptions.ForThis.Get("volume", 1.0);

// Shared configuration (across all mods)
ModOptions.ForAllMods.Set("globalSetting", "value");
```

### Logging

Structured logging with LibLog:

```csharp
using Ducky.Sdk.Logging;

Log.Info("Player joined: {PlayerName}", playerName);
Log.Warn("Low health: {Health}", health);
Log.Error(exception, "Failed to load resource: {ResourceId}", resourceId);
```

## Shared Runtime APIs

### ModInfo: the running mod's identity

```csharp
using Ducky.Sdk.Utils;

string name = ModInfo.Name;              // mod name
string id = ModInfo.Id;                  // "steam.<digits>" or "local.<folder name>"
string? folder = ModInfo.Folder;         // full path of the mod folder, null when unknown
string? assets = ModInfo.AssetsPath;     // <Folder>/assets, null when Folder is null
bool core = ModInfo.IsHotReloadCore;     // hot reload Core (<ModName>.Core.<timestamp>)
```

A hot reload Core has no file path, so `Folder` uses the folder of the already loaded shim assembly of the same mod.
Note: the default build copies the **contents** of `AssetsDir` into the mod folder root and does not create an `assets` subfolder. `AssetsPath` follows the specification (`Folder/assets`), so check that the directory exists before using it.

### MainThread: from any thread to Unity's main thread

```csharp
using Ducky.Sdk.Threading;

MainThread.Post(() => DoSomethingOnMainThread());          // queued; runs on the main thread in a later frame

await MainThread.InvokeAsync(() => GameObject.Find("x"),   // returns Task<T> with the result or exception
                             cancellationToken,
                             TimeSpan.FromSeconds(2));     // timeout is optional
```

- Work **never runs inline**, even when called from the main thread. `Post` never blocks the caller.
- Timeout before the work starts: the task faults with `TimeoutException` and the work never runs. Timeout after it started: the task completes with the work's own result or exception.
- Cancellation before the work starts: the task is cancelled and the work does not run.
- `ModBehaviourBase` starts the scheduler when the mod is enabled and closes it when the last enabled subclass is disabled (pending `InvokeAsync` tasks are cancelled and pending `Post` actions are discarded).
- A mod that does not derive from `ModBehaviourBase` must call `MainThread.EnsureStarted()` on the main thread first; until then posted work waits.

### Subscriptions: event subscriptions released with the mod

```csharp
using Ducky.Sdk.GameEvents;

protected override void ModEnabled()
{
    Subscriptions.Add(LevelEvents.OnInitialized(() => OnLevelReady()));
    Subscriptions.Add(SceneEvents.OnPhaseChanged((previous, next) => OnPhase(next)));
    Subscriptions.Add(() => SomeOtherEvent -= handler);     // any Action works too
}
```

- Released after `ModDisabled()`, in reverse order of addition. Released even when `ModDisabled()` throws, and the original exception is still rethrown. Hot reload releases the old Core's subscriptions the same way.
- An item added while the behaviour is disabled is disposed at once and is not kept for the next enable.
- A failing disposal is logged; the other subscriptions are still released.
- `ScenePhases.Of(sceneName)` maps names starting with `Base` to `Base`, names starting with `Level` to `Raid`, and everything else to `Other`. Matching is case-sensitive.

**Cleanup checklist**: event handlers added through `Subscriptions` need no `-=` in `ModDisabled`. These still need explicit cleanup in `ModDisabled`: Harmony patches, `GameObject`s, buff registrations and UI.

### Analyzer diagnostics DUCKY006 / DUCKY007

| ID | Meaning | Remedy |
| --- | --- | --- |
| `DUCKY006` | A Unity API is used inside a delegate passed to `Task.Run` or `UniTask.RunOnThreadPool` | `await UniTask.SwitchToMainThread()` first, or use `MainThread.InvokeAsync` |
| `DUCKY007` | A Unity object is accessed in a static constructor or a static field/property initializer | Create it lazily on the main thread in an instance member |

Both are warnings, enabled by default; they do not fail the build. Generated code is not analyzed. Known limits: Unity calls in helper methods and method groups are not followed. `Debug`, `Mathf` and structs such as `Vector3` are not flagged; `Debug.Log*` is covered by `DUCKY001`–`DUCKY005`.

### Bullet count

`ItemSetting_Gun` has `TryGetBulletCount(out int)` and `SetBulletCount(int)`, which read and write the game's internal `bulletCount` member (a private instance property). When the member is not found, a single log line is written; `SetBulletCount` then does nothing and returns the same gun, so calls can be chained.

### Game mechanics reference

[docs/game-mechanics.md](docs/game-mechanics.md) records what the game does (buff resistance, enemy spawn timing, the
`Health.Hurt` damage order, explosions, weather and clock, rolling and stamina, loot boxes) with the evidence for each
statement, and describes the helpers that build on it (`BuffImmunity`, `StatBuff`, `PlayerCombat`, `HurtRules`,
`LethalDamageGuard`, `EnemyReady`, and others). The Harmony-based helpers need `IncludeHarmony` set to true. The development
hot reload is described in [docs/hot-reload.md](docs/hot-reload.md).

## Advanced Features

### Harmony Runtime Patching

Enable runtime method patching for advanced modding:

```xml
<PropertyGroup>
  <ModName>MyAwesomeMod</ModName>
  <IncludeHarmony>true</IncludeHarmony>
</PropertyGroup>
```

Use Harmony patches:

```csharp
using HarmonyLib;

public class ModBehaviour : ModBehaviourBase
{
    private Harmony _harmony;
    
    protected override void ModEnabled()
    {
        _harmony = new Harmony("com.myname.mymod");
        _harmony.PatchAll();
    }
    
    protected override void ModDisabled()
    {
        _harmony?.UnpatchAll();
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.TakeDamage))]
public static class Player_TakeDamage_Patch
{
    static void Prefix(Player __instance, ref float damage)
    {
        Log.Info("Player taking damage: {Damage}", damage);
        damage *= 0.5f; // Reduce damage by 50%
    }
}
```

### Assembly Merging

By default, the SDK merges all dependencies into a single DLL:

```xml
<PropertyGroup>
  <!-- Default: true (single-DLL distribution) -->
  <EnableILRepack>true</EnableILRepack>
  
  <!-- Disable merging (dependencies copied to Dependency/ folder) -->
  <EnableILRepack>false</EnableILRepack>
</PropertyGroup>
```

Benefits:
- ✅ Single-file distribution
- ✅ No version conflicts between mods
- ✅ Internalized dependencies (no namespace pollution)
- ✅ Smaller deployment footprint

### Multi-Project Mods

Organize complex mods into multiple projects:

**Shared Library Project** (`MyMod.Common.csproj`):

```xml
<PropertyGroup>
  <IsModLib>true</IsModLib>
  <AssetsDir>$(SolutionDir)/MyMod/assets</AssetsDir>
</PropertyGroup>
```

**Entry Project** (`MyMod.csproj`):

```xml
<PropertyGroup>
  <ModName>MyMod</ModName>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="../MyMod.Common/MyMod.Common.csproj" />
</ItemGroup>
```

### Automated Asset Generation

The SDK automatically generates:

1. **info.ini** - Basic mod metadata (name, displayName, description)
2. **preview.png** - 256x256 preview: one of ten bundled backgrounds, chosen by a SHA-256 hash of the mod name, with the project abbreviation drawn on top (e.g. `Ducky.EntranceMod` → `DEM`). An existing `preview.png` is never overwritten, and library projects (`IsModLib`) get none. The backgrounds were generated once with ComfyUI and are committed; maintainers regenerate them with `scripts/generate_preview_backgrounds.sh`. Mod builds never call ComfyUI.
3. **publishedFileId** - Steam Workshop ID synchronization

Disable auto-deployment during development:

```xml
<PropertyGroup>
  <DeployMod>false</DeployMod>
</PropertyGroup>
```

## Project Structure

```
MyMod/
├── MyMod.csproj           # Main mod project
├── ModBehaviour.cs        # Mod entry point
├── LK.cs                  # Localization keys
├── Local.props            # Git-ignored local config
└── assets/
    ├── info.ini           # Mod metadata
    ├── preview.png        # Mod preview (background + abbreviation, auto-generated)
    ├── description.md     # Long description
    └── Locales/
        ├── en.csv         # English translations
        ├── zh.csv         # Chinese translations
        └── ...
```

## SDK Development

### Building the SDK Locally

1. **Package to local feed:**

```bash
./scripts/packToLocal.sh --version 0.0.1
```

2. **Rebuild samples with fresh SDK:**

```bash
./scripts/rebuild_samples.sh
```

3. **Fetch game dependencies:**

```bash
./scripts/fetch_build_dependency.sh
```

### Testing Changes

The `Samples/` directory contains integration test projects:

- **Ducky.SingleProject** - Single-project mod template
- **Ducky.EntranceMod** - Multi-project mod with shared library
- **Ducky.TryHarmony** - Harmony patching example

Run `./scripts/rebuild_samples.sh` to validate end-to-end SDK workflow.

### Repository Structure

```
Ducky.Sdk/
├── Sdk/                           # SDK development workspace
│   ├── SDKlibs/
│   │   ├── Ducky.Sdk/            # Core NuGet package
│   │   │   ├── Ducky.Sdk.nuspec  # Package manifest
│   │   │   ├── Ducky.Sdk.props   # MSBuild properties
│   │   │   ├── Ducky.Sdk.targets # Build targets
│   │   │   └── Ducky.Sdk.Orchestration.targets # Calls the build runner
│   │   └── Ducky.Sdk.Lib/        # Shared library (distributed as source)
│   ├── Ducky.Sdk.Analyser/       # Roslyn source generator
│   └── Tests/                     # Unit tests
├── Samples/                       # Example mod projects
│   ├── Ducky.SingleProject/
│   ├── Ducky.EntranceMod/
│   └── Ducky.TryHarmony/
├── scripts/                       # Build automation
│   ├── packToLocal.sh            # Package SDK to local feed
│   ├── rebuild_samples.sh        # Rebuild samples with fresh SDK
│   └── fetch_build_dependency.sh # Download game assemblies
└── duckylocal/                   # Local NuGet feed
```

## Configuration Reference

### MSBuild Properties

| Property | Default | Description |
|----------|---------|-------------|
| `ModName` | (required) | Mod identifier and output DLL name |
| `SteamFolder` | - | Path to Steam installation |
| `DuckovFolder` | Computed | Game directory (auto-computed from SteamFolder) |
| `ManagedDirectory` | Computed | Game managed assemblies directory (can be explicitly set for CI environments) |
| `ModsDirectory` | Computed | Mod deployment target directory (can be explicitly set for CI environments) |
| `DeployMod` | `true` | Enable automatic deployment to game |
| `EnableILRepack` | `true` | Merge assemblies into single DLL |
| `IncludeHarmony` | `false` | Include Harmony for runtime patching |
| `AssetsDir` | `assets/` | Custom assets directory path |
| `ExcludeSdkLib` | `true` | Exclude SDK source compilation (for entry projects) |
| `IsModLib` | `false` | Mark project as shared library |

### Localization Attributes

#### `[LanguageSupport(...)]`

Specify supported languages:

```csharp
[LanguageSupport("en", "zh", "fr", "de", "ja")]
public static class LK { ... }
```

Use "all" to generate files for all supported languages:

```csharp
[LanguageSupport("all")]
public static class LK { ... }
```

Supported languages when using "all": de, en, es, fr, ja, ko, pt, ru, zh-hant, zh

Mix "all" with additional languages:

```csharp
[LanguageSupport("all", "custom-lang")]
public static class LK { ... }
```

#### `[TranslateFile]` or `[TranslateFile("ext")]`

Store translations in external files:

```csharp
[TranslateFile]           // Uses .txt extension
public const string Help = "Help Text";

[TranslateFile("md")]     // Uses .md extension
public const string ReadMe = "ReadMe Content";
```

## Troubleshooting

### "SteamDir property must be set"

**Local Development:** Create `Local.props` in your project root with your Steam installation path:

```xml
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/steam/</SteamFolder>
  </PropertyGroup>
</Project>
```

**CI Environments:** Set `ManagedDirectory` and `ModsDirectory` environment variables:

```yaml
env:
  ManagedDirectory: /path/to/managed/
  ModsDirectory: /path/to/mods/
```

Or via MSBuild parameters:

```bash
dotnet build -p:ManagedDirectory=/path/to/managed/ -p:ModsDirectory=/path/to/mods/
```

### Stale NuGet Cache

Clear all caches and rebuild:

```bash
./scripts/rebuild_samples.sh --clear-all-caches
```

Or manually:

```bash
dotnet nuget locals all --clear
rm -rf ~/.nuget/packages/ducky.sdk/
```

### Missing Game Assemblies

Download required game DLLs:

```bash
./scripts/fetch_build_dependency.sh
```

### Mod Not Deploying

1. Verify `$(DuckovFolder)` path exists
2. Check `$(DeployMod)` is not set to `false`
3. Ensure write permissions to game directory

### Localization Keys Missing in CSV

The SDK validates CSV files contain all keys. Run:

```bash
dotnet build
```

Check build output for validation errors.

## Examples

See the `Samples/` directory for complete examples:

- **[Ducky.SingleProject](Samples/Ducky.SingleProject/)** - Minimal single-file mod
- **[Ducky.EntranceMod](Samples/Ducky.EntranceMod/)** - Multi-project with localization
- **[Ducky.TryHarmony](Samples/Ducky.TryHarmony/)** - Runtime patching with Harmony

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test with `./scripts/rebuild_samples.sh`
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

- **Game**: "Escape from Duckov" by TeamSoda
- **Harmony**: [Harmony Library](https://github.com/pardeike/Harmony)
- **ILRepack**: [ILRepack.Lib](https://github.com/gluck/il-repack) (called in process by the build runner)

## Support

- 🐛 [Report Issues](https://github.com/ducky7go/Ducky.Sdk/issues)
- 💬 [Discussions](https://github.com/ducky7go/Ducky.Sdk/discussions)

---

Made with ❤️ for the Escape from Duckov modding community
