# Ducky.Sdk - AI Coding Agent Instructions

## Project Overview

**Ducky.Sdk** is a .NET SDK for developing mods for "Escape from Duckov" game using the BetterModLoader framework. It provides:
- A NuGet package containing source generators, MSBuild tasks, and shared libraries
- Automatic localization key generation from constants
- Build-time deployment to game directories
- Automated mod metadata generation (info.ini, preview.png)
- Assembly merging via ILRepack for single-DLL distribution
- Strongly-typed mod development patterns

## Architecture

### Repository Structure
- **`Sdk/`** - SDK development workspace with main solution (`Docky.Sdk.slnx`)
  - `SDKlibs/Ducky.Sdk/` - Core NuGet package (`.nuspec`, `.props`, `.targets`)
  - `SDKlibs/Ducky.Sdk.Lib/` - Shared library source code (distributed via contentFiles)
  - `Ducky.Sdk.Analyser/` - Roslyn incremental source generator
  - `Tests/` - Unit tests
- **`Samples/`** - Example mod projects consuming the SDK (`Docky.Sdk.Sample.slnx`)
- **`scripts/`** - Build automation (packToLocal.sh, rebuild_samples.sh, fetch_build_dependency.sh)
- **`duckylocal/`** - Local NuGet feed for testing

### Key Design Pattern: Source-Code Distribution
The SDK uses `<developmentDependency>true</developmentDependency>` in the nuspec and distributes `.cs` files via `contentFiles/cs/netstandard2.1/` rather than compiled assemblies. This allows mod developers to:
- See and debug SDK code directly in their project
- Avoid assembly version conflicts with game assemblies
- Have single-DLL mod outputs without SDK dependencies

## Critical Development Workflows

### Building and Testing SDK Changes

```bash
# 1. Package SDK to local feed (version 0.0.1 for samples)
./scripts/packToLocal.sh --version 0.0.1

# 2. Rebuild samples with fresh SDK (clears caches automatically)
./scripts/rebuild_samples.sh

# 3. For production packaging with custom version
./scripts/packToLocal.sh --version 1.2.3 --configuration Release

# 4. Clear all cached versions (not just the specific version)
./scripts/packToLocal.sh --version 0.0.1 --purge-all-versions

# 5. Clear all NuGet caches including HTTP cache
./scripts/rebuild_samples.sh --clear-all-caches
```

**Important**: `rebuild_samples.sh` performs comprehensive cache cleanup:
- Clears version-specific NuGet global-packages cache
- Clears NuGet HTTP metadata cache (ensures fresh package info)
- Deletes all bin/obj folders in Samples
- Uses `--no-cache --force` on restore to bypass MSBuild caching

### Local Development Setup

Mod projects require `Local.props` (git-ignored) to specify game location:

```xml
<!-- Samples/Local.props -->
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/steam/</SteamFolder>
    <!-- SDK computes: $(DuckovFolder) = $(SteamFolder)steamapps/common/Escape from Duckov/ -->
  </PropertyGroup>
</Project>
```

Without `Local.props`, builds fail with: _"SteamDir property must be set"_ (see `Samples/Directory.Build.props` validation).

### Fetching Game Dependencies

Game assemblies (Unity, TeamSoda, etc.) are not in the repo. Use:

```bash
./scripts/fetch_build_dependency.sh
```

Downloads game DLLs from the `sdk_build_dependency` GitHub releases to `./Managed/`. The SDK's `.props` file dynamically references these via `AddDuckyManagedReferences` target.

## Mod Development Conventions

### ModBehaviour Pattern
Every mod has a class inheriting `ModBehaviourBase` (from SDK):

```csharp
public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()  { /* Apply patches, register events */ }
    protected override void ModDisabled() { /* Clean up, unpatch */ }
}
```

The SDK base class auto-initializes:
- Logging via `Log.Current = LogProvider.GetLogger(modName)`
- Localization via `L.Instance.SetLanguage(...)`
- Buff registration via `BuffRegistrator.Instance.EnsureBuffIdRegion()`

### Localization System (LK → L Pattern)

1. **Define keys** in a static `LK` class with nested groups:
   ```csharp
   using Ducky.Sdk.Attributes;
   
   [LanguageSupport("en", "zh", "fr")]  // Specify supported languages
   public static class LK
   {
       public static class UI
       {
           public const string NiceWelcomeMessage = "Welcome to Ducky Entrance Mod!";
           
           [TranslateFile("md")]  // Long text stored in separate .md files
           public const string DetailedDescription = "Detailed Description";
       }
   }
   ```

2. **Source generator** (`DuckyLocalizationGenerator`) creates matching `L` properties:
   ```csharp
   // Auto-generated partial class L
   public static class UI
   {
       public static string NiceWelcomeMessage => Get(LK.UI.NiceWelcomeMessage);
       public static string DetailedDescription => Get(LK.UI.DetailedDescription);
   }
   ```

3. **CSV files** in `assets/Locales/` (e.g., `en.csv`, `zh.csv`) map keys to translations:
   ```csv
   Key,Value
   Welcome to Ducky Entrance Mod!,Welcome!
   Detailed Description,"DetailedDescription.md"
   ```

4. **File-based translations** for long content (marked with `[TranslateFile]`):
   - CSV value points to filename (e.g., `DetailedDescription.md`)
   - Actual content stored in `Locales/{lang}/DetailedDescription.md`
   - Supports `.md` and `.txt` extensions

5. **MSBuild task** (`UpdateLocalesCsv` target) validates CSV files contain all keys using `LKeys.All` and `LocalsKeysHash.Hash` generated by the source generator.

### Project Configuration

**Mod entry project** (produces the DLL):
```xml
<PropertyGroup>
  <ModName>Ducky.EntranceMod</ModName>
  <ExcludeSdkLib>true</ExcludeSdkLib> <!-- Don't compile SDK sources directly -->
</PropertyGroup>
<PackageReference Include="Ducky.Sdk" Version="0.0.1">
  <PrivateAssets>all</PrivateAssets> <!-- Build-time only -->
</PackageReference>
```

**Shared library** (for multi-project mods):
```xml
<PropertyGroup>
  <IsModLib>true</IsModLib>
  <AssetsDir>$(SolutionDir)/Ducky.EntranceMod/assets</AssetsDir>
</PropertyGroup>
```

### Build Outputs and Deployment

On build, the SDK's `.targets` file executes these steps in order:

1. **EnsureInfoIni** - Generates basic `info.ini` if missing (with name, displayName, description)
2. **GeneratePreview** - Creates 256x256 identicon-style `preview.png` based on mod name hash (if missing)
3. **CollectFromMod** - Syncs `publishedFileId` from deployed mod back to source `info.ini` (for Steam Workshop)
4. **CopyToDuckov** - Deploys assets/ folder (info.ini, description.md, preview.png, Locales/) to game's Mods directory
5. **CopyMissingDependencies** - Copies non-game DLLs to `Dependency/` subfolder
6. **PackModWithILRepack** - (Optional, if `EnableILRepack=true`) Merges all assemblies into single DLL

Set `<DeployMod>false</DeployMod>` to disable auto-deployment during development.

### Automated Asset Generation

The SDK automatically generates missing mod metadata:

**info.ini Generation** (`ensure-info-ini.csx`):
- Runs before any other assets operations
- Creates basic info.ini if it doesn't exist
- Sets `name`, `displayName`, and `description` fields
- Allows manual customization (won't overwrite existing files)

**preview.png Generation** (`generate-preview.csx`):
- Generates GitHub-style identicon (5x5 symmetric grid)
- Uses SHA256 hash of mod name for deterministic colors
- 256x256 PNG with complementary color scheme
- Skips if preview.png already exists (preserves custom artwork)

**publishedFileId Sync** (`collect-from-mod.csx`):
- Reads `publishedFileId` from deployed mod's info.ini
- Writes back to source assets/info.ini if not present
- Enables version control of Steam Workshop IDs

## Code Patterns and Conventions

### Logging
```csharp
using Ducky.Sdk.Logging;
Log.Info("Message with {Param}", value);  // Structured logging via LibLog
```

### Mod Identity
- `Helper.GetModName()` - Reads `[ModName]` attribute or assembly name
- `Helper.GetModId()` - Returns `ModId` struct: "local.FolderName" for local mods, "steam.WorkshopId" for Steam Workshop

### Configuration Storage
```csharp
using Ducky.Sdk.Options;
// Per-mod config: Application.persistentDataPath/Ducky/ModLocalConfig/ModName.json
ModOptions.ForThis.Set("key", value);
// Shared config: Application.persistentDataPath/Ducky/ModsLocalConfig.json  
ModOptions.ForAllMods.Set("key", value);
```

## Important MSBuild Mechanics

### Target Execution Order
1. **BeforeBuild Phase**:
   - `ValidateDuckovFolder` - Ensures either DuckovFolder or SteamFolder is set
   - `WarnMissingManagedDirectory` - Warns if game assemblies not found

2. **Build Phase**:
   - `CoreCompile` - Compiles source code, runs source generators
   - `ExtractLKeysJson` (AfterTargets="CoreCompile") - Extracts localization keys to JSON

3. **AfterBuild Phase**:
   - `EnsureInfoIni` - Generates info.ini if missing
   - `GeneratePreview` - Creates preview.png if missing
   - `CollectFromMod` - Syncs publishedFileId from deployed mod
   - `CopyToDuckov` - Deploys assets and DLL to game directory
   - `CopyMissingDependencies` - Copies non-game dependencies
   - `PackModWithILRepack` - (Optional) Merges assemblies
   - `UpdateLocalesCsv` - Validates localization CSV files

### MSBuild Properties
- **$(SteamFolder)** - Path to Steam installation (set in Local.props)
- **$(DuckovFolder)** - Computed game directory path
- **$(ModName)** - Mod identifier (required)
- **$(DeployMod)** - Set to `false` to skip deployment
- **$(EnableILRepack)** - Set to `true` to enable assembly merging
- **$(AssetsDir)** - Custom assets directory (defaults to `assets/`)

### ContentFiles Visibility
SDK sources are hidden from Solution Explorer:
```xml
<Compile Update="@(_DuckySdkCompileForVisibility)">
  <Visible>false</Visible>
</Compile>
```

## Testing Strategy

- Unit tests in `Sdk/Tests/Ducky.Sdk.Lib.Tests/`
- Sample projects serve as integration tests (`Samples/Ducky.SingleProject/`, `Samples/Ducky.EntranceMod/`)
- Use `rebuild_samples.sh` to validate end-to-end SDK workflow

## Package Versioning

- **Development**: Always use `0.0.1` for samples (hardcoded in `rebuild_samples.sh`)
- **Production**: Use `--version x.y.z` with `packToLocal.sh` or rely on MinVer in CI (`Directory.Packages.props` includes MinVer when `$(CI) == 'true'`)

## Common Pitfalls

1. **"SteamDir property must be set"** - Create `Samples/Local.props` with `<SteamFolder>`
2. **Stale NuGet cache after SDK changes** - Always use `./scripts/rebuild_samples.sh` which clears:
   - Version-specific global-packages cache
   - HTTP metadata cache
   - All bin/obj directories
3. **Missing game assemblies** - Run `fetch_build_dependency.sh` before building
4. **Localization keys missing in CSV** - Generator creates `LKeys.All` array; MSBuild task validates CSV completeness
5. **Mod not deploying** - Check `$(DeployMod)` property and `$(DuckovFolder)` path validity
6. **Preview.png not generating** - Ensure `info.ini` exists with `name` field; check build logs for script errors
7. **ILRepack not merging** - Set `<EnableILRepack>true</EnableILRepack>` in project file and ensure ilrepack is installed globally
8. **Old SDK cached during development** - Use `--purge-all-versions` flag or manually delete `~/.nuget/packages/ducky.sdk/`

## Localization Attributes

### `[LanguageSupport(...)]`
Applied to the `LK` class to specify which languages should be supported:
```csharp
[LanguageSupport("en", "zh", "fr", "de")]
public static class LK { ... }
```
- Generates CSV files only for specified languages
- If omitted, discovers languages from existing files or defaults to English
- Language codes: "en" (English), "zh" (Chinese), "fr" (French), "de" (German), "ja" (Japanese), etc.

### `[TranslateFile]` and `[TranslateFile("ext")]`
Applied to const string fields to indicate the value should reference an external file:
```csharp
[TranslateFile]           // Defaults to .txt
public const string HelpText = "Help Text";

[TranslateFile("md")]     // Uses .md extension
public const string ReadMe = "ReadMe";
```
- CSV value becomes filename: `HelpText.txt`, `ReadMe.md`
- Actual translated content stored in `Locales/{lang}/HelpText.txt`
- Supports `"md"` and `"txt"` extensions
- Files are auto-created if missing (won't overwrite manual edits)
- Useful for long descriptions, changelogs, or formatted content

## File Naming Conventions

- Source generators output: `L.GeneratedKeys.{Namespace}.g.cs`, `LKeys.{Namespace}.g.cs`
- Mod assets folder: `assets/` (case-sensitive on Linux)
- Localization CSVs: `en.csv`, `zh.csv` (language codes)
- Translation files: `Locales/{lang}/KeyName.{ext}` (for keys with `[TranslateFile]`)
- SDK scripts: `*.csx` files in `scripts/` directory (dotnet-script format)
  - `ensure-info-ini.csx` - Generates basic info.ini
  - `generate-preview.csx` - Creates identicon preview image
  - `collect-from-mod.csx` - Syncs publishedFileId
  - `extract-lkeys-json.csx` - Extracts localization keys
  - `update-locales-csv.csx` - Updates localization CSV files and generates translation files

## When Modifying the SDK

1. **Analyser changes**: Rebuild `Ducky.Sdk.Analyser` project
2. **Library changes**: Edit files in `SDKlibs/Ducky.Sdk.Lib/` (no rebuild needed, packed as source)
3. **Build logic changes**: Edit `Ducky.Sdk.props` or `Ducky.Sdk.targets`
4. **Script changes**: Edit `.csx` files in `scripts/` directory (pure C#, no compilation needed)
5. **Testing workflow**:
   ```bash
   # After any SDK modification:
   ./scripts/rebuild_samples.sh
   
   # This will:
   # - Pack the SDK with latest changes
   # - Clear all relevant caches
   # - Rebuild all sample projects
   # - Validate the changes work end-to-end
   ```
6. Always test via `./scripts/rebuild_samples.sh` before committing

## External Dependencies

- **Game**: "Escape from Duckov" (Unity game, references via `Managed/` folder)
- **Mod Loader**: BetterModLoader (provides `ModBehaviour` base class)
- **Build Tools**: 
  - .NET SDK 9.0 (see `Sdk/global.json`)
  - dotnet-script (for running .csx automation scripts)
  - ilrepack (optional, for assembly merging - install globally with `dotnet tool install -g ilrepack`)
- **Image Libraries**: SixLabors.ImageSharp 3.1.6 (for cross-platform preview.png generation)
- **Package Management**: Central Package Management enabled (`Directory.Packages.props`)

## Script Dependencies and Cross-Platform Support

All automation scripts (`*.csx`) are written in pure C# using dotnet-script and are cross-platform compatible:
- **Linux/macOS**: Primary development platform, fully tested
- **Windows**: Compatible (uses bash scripts via Git Bash or WSL)
- **Image Generation**: Uses SixLabors.ImageSharp (pure managed code, no native dependencies)
- **File Operations**: Uses System.IO with proper path handling for cross-platform compatibility
