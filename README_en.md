# Ducky.Sdk

**[中文](README.md) | English**

A comprehensive .NET SDK for developing mods for "Escape from Duckov" game using the BetterModLoader framework.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download)

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

- .NET SDK 9.0 or later
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
2. **preview.png** - 256x256 identicon based on mod name
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
    ├── preview.png        # Mod icon
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
│   │   │   └── scripts/*.csx     # Automation scripts
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

Create `Local.props` in your project root with your Steam installation path:

```xml
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/steam/</SteamFolder>
  </PropertyGroup>
</Project>
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
- **Mod Loader**: BetterModLoader
- **Harmony**: [Harmony Library](https://github.com/pardeike/Harmony)
- **ILRepack**: [dotnet-ilrepack](https://github.com/gluck/il-repack)

## Support

- 🐛 [Report Issues](https://github.com/ducky7go/Ducky.Sdk/issues)
- 💬 [Discussions](https://github.com/ducky7go/Ducky.Sdk/discussions)

---

Made with ❤️ for the Escape from Duckov modding community
