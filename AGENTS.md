# Ducky.Sdk - AI Coding Agent Instructions

## Project Status (Updated: 2025-11-20)

**Current Version**: Development (0.0.1 for local testing)  
**Target Framework**: .NET Standard 2.1  
**Build System**: .NET SDK 9.0

### Recent Changes
- ✅ Removed BetterModLoader framework dependency - SDK now provides standalone mod infrastructure
- ✅ Improved source code distribution pattern for better IDE integration
- ✅ Enhanced localization system with file-based translation support
- ✅ Stabilized ILRepack assembly merging workflow
- ✅ Implemented comprehensive CI/CD pipeline with automated NuGet publishing

### SDK Core Components
The SDK consists of **3 main components**:
1. **Ducky.Sdk.Analyser** - Roslyn incremental source generator for localization
2. **Ducky.Sdk.Lib** - Shared library code distributed as source files
3. **Ducky.Sdk** - NuGet package with MSBuild integration (.props, .targets, scripts)

### Development Focus
- Maintaining backward compatibility with existing mods
- Improving developer experience through better tooling
- Enhancing build-time automation and validation
- Optimizing source generator performance

## Project Overview

**Ducky.Sdk** is a comprehensive .NET SDK for developing mods for "Escape from Duckov" game. It provides:
- A NuGet package containing source generators, MSBuild tasks, and shared libraries
- Automatic localization key generation from constants
- Build-time deployment to game directories
- Automated mod metadata generation (info.ini, preview.png)
- Assembly merging via ILRepack for single-DLL distribution
- Optional Harmony runtime patching support (0Harmony)
- Strongly-typed mod development patterns
- Automatic NuGet dependency copying and merging

## Architecture

### SDK Package Structure

The SDK is distributed as a single NuGet package (`Ducky.Sdk`) containing three integrated components:

#### 1. Ducky.Sdk.Analyser (Roslyn Source Generator)
**Location**: `Sdk/Ducky.Sdk.Analyser/`  
**Output**: `analyzers/cs/Ducky.Sdk.Analyser.dll` in NuGet package  
**Target Framework**: .NET Standard 2.0 (Roslyn requirement)

**Responsibilities**:
- Implements `IIncrementalGenerator` for compile-time code generation
- Scans for `LK` class with localization key constants
- Generates matching `L` class with properties for runtime localization
- Produces `LKeys.All` array and `LocalsKeysHash.Hash` for CSV validation
- Outputs to `obj/` directory: `L.GeneratedKeys.{Namespace}.g.cs`, `LKeys.{Namespace}.g.cs`

**Key Features**:
- Incremental generation for fast rebuilds
- Supports nested classes for key organization
- Handles `[LanguageSupport]` and `[TranslateFile]` attributes
- Generates JSON metadata for MSBuild tasks

**Build Integration**:
```xml
<!-- Automatically included via analyzers/cs/ folder in NuGet -->
<Analyzer Include="$(NuGetPackageRoot)ducky.sdk/.../Ducky.Sdk.Analyser.dll" />
```

#### 2. Ducky.Sdk.Lib (Shared Library Source Code)
**Location**: `Sdk/SDKlibs/Ducky.Sdk.Lib/`  
**Distribution**: `contentFiles/cs/netstandard2.1/Sdk/` in NuGet package  
**Target Framework**: .NET Standard 2.1

**Responsibilities**:
- Provides `ModBehaviourBase` - base class for all mods
- Implements localization runtime (`L` class partial, `LocalizationManager`)
- Logging infrastructure (`Log`, `LogProvider` via LibLog pattern)
- Configuration management (`ModOptions.ForThis`, `ModOptions.ForAllMods`)
- Mod identity helpers (`Helper.GetModName()`, `Helper.GetModId()`)
- Buff registration system (`BuffRegistrator`)

**Distribution Strategy**:
- **Source-only distribution** - `.cs` files copied to consuming projects
- Marked as `<Visible>false</Visible>` in Solution Explorer
- Compiled into mod assembly (not SDK assembly)
- Avoids assembly version conflicts with game DLLs

**Key Classes**:
```
Ducky.Sdk.Lib/
├── Attributes/
│   ├── LanguageSupportAttribute.cs
│   └── TranslateFileAttribute.cs
├── Localizations/
│   ├── L.cs (partial, combined with generated code)
│   └── LocalizationManager.cs
├── Logging/
│   ├── Log.cs
│   └── LogProvider.cs
├── ModBehaviours/
│   └── ModBehaviourBase.cs
├── Options/
│   └── ModOptions.cs
└── Helpers/
    └── Helper.cs
```

#### 3. Ducky.Sdk (MSBuild Integration)
**Location**: `Sdk/SDKlibs/Ducky.Sdk/`  
**Files in NuGet**:
- `build/Ducky.Sdk.props` - MSBuild properties and references
- `build/Ducky.Sdk.targets` - Build targets and tasks
- `scripts/*.csx` - C# automation scripts (dotnet-script)
- `tools/0Harmony.dll` - Optional Harmony library

**Ducky.Sdk.props Responsibilities**:
- Defines default properties (`EnableILRepack=true`, `DeployMod=true`)
- Adds game assembly references from `$(DuckovFolder)/Managed/`
- Conditionally includes Harmony reference when `$(IncludeHarmony)=true`
- Hides SDK source files from Solution Explorer
- Sets `CopyLocalLockFileAssemblies=true` for dependency merging

**Ducky.Sdk.targets Responsibilities**:
- Validates `$(SteamFolder)` or `$(DuckovFolder)` is set
- Executes build pipeline in order:
  1. `EnsureInfoIni` - Generate `assets/info.ini` if missing
  2. `GeneratePreview` - Create `assets/preview.png` identicon
  3. `ExtractLKeysJson` - Export localization keys to JSON
  4. `UpdateLocalesCsv` - Validate/update CSV files
  5. `CollectFromMod` - Sync `publishedFileId` from deployed mod
  6. `CopyToDuckov` - Deploy to game directory
  7. `PackModWithILRepack` - Merge assemblies (if enabled)

**Automation Scripts** (`scripts/*.csx`):
- `ensure-info-ini.csx` - Generates basic mod metadata
- `generate-preview.csx` - Creates 256x256 identicon using ImageSharp
- `collect-from-mod.csx` - Reads Steam Workshop ID from deployed mod
- `extract-lkeys-json.csx` - Exports localization keys for validation
- `update-locales-csv.csx` - Updates CSV files and creates translation files

### Repository Structure
- **`Sdk/`** - SDK development workspace
  - `Docky.Sdk.slnx` - Main solution file
  - `SDKlibs/Ducky.Sdk/` - NuGet package files (.nuspec, .props, .targets)
  - `SDKlibs/Ducky.Sdk.Lib/` - Shared library source code
  - `Ducky.Sdk.Analyser/` - Roslyn source generator project
  - `Tests/` - Unit tests for SDK components
  - `Directory.Packages.props` - Central package version management
  - `global.json` - .NET SDK version pinning
- **`Samples/`** - Test projects for SDK validation (temporary verification only)
  - `Docky.Sdk.Sample.slnx` - Samples solution
  - Various mod projects for testing different SDK features
- **`scripts/`** - Build automation scripts
  - `packToLocal.sh` - Package SDK to local NuGet feed
  - `rebuild_samples.sh` - Test SDK changes end-to-end
  - `fetch_build_dependency.sh` - Download game assemblies
- **`duckylocal/`** - Local NuGet feed for testing
- **`.github/workflows/`** - CI/CD automation

### Key Design Pattern: Source-Code Distribution
The SDK uses `<developmentDependency>true</developmentDependency>` in the nuspec and distributes `.cs` files via `contentFiles/cs/netstandard2.1/` rather than compiled assemblies. This allows mod developers to:
- See and debug SDK code directly in their project
- Avoid assembly version conflicts with game assemblies
- Have single-DLL mod outputs without SDK dependencies

## Critical Development Workflows

### SDK Development Cycle

When modifying the SDK, follow this workflow to ensure changes work correctly:

#### 1. Make Changes to SDK Components

**Analyser changes** (`Sdk/Ducky.Sdk.Analyser/`):
- Modify source generator logic
- Build project to compile analyzer DLL
- Changes affect compile-time code generation

**Library changes** (`Sdk/SDKlibs/Ducky.Sdk.Lib/`):
- Edit `.cs` files directly
- No rebuild needed - distributed as source code
- Changes immediately available after packaging

**Build logic changes** (`Sdk/SDKlibs/Ducky.Sdk/`):
- Edit `Ducky.Sdk.props` for MSBuild properties
- Edit `Ducky.Sdk.targets` for build targets
- Edit `.csx` scripts for automation tasks

#### 2. Package SDK to Local Feed

```bash
# Package with development version (for testing)
./scripts/packToLocal.sh --version 0.0.1

# Package with custom version
./scripts/packToLocal.sh --version 1.2.3-alpha

# Package for release (with Release configuration)
./scripts/packToLocal.sh --version 1.2.3 --configuration Release

# Clear all cached versions before packaging
./scripts/packToLocal.sh --version 0.0.1 --purge-all-versions
```

**What `packToLocal.sh` does**:
1. Builds `Ducky.Sdk.Analyser` project (Release or Debug)
2. Copies analyzer DLL to `Sdk/SDKlibs/Ducky.Sdk/` for packaging
3. Runs `nuget pack` with specified version
4. Outputs `.nupkg` to `duckylocal/` directory
5. Optionally clears NuGet caches

#### 3. Validate SDK Changes

```bash
# Rebuild all test projects with fresh SDK
./scripts/rebuild_samples.sh

# Clear all caches before rebuilding (recommended after SDK changes)
./scripts/rebuild_samples.sh --clear-all-caches
```

**What `rebuild_samples.sh` does**:
1. Packages SDK with version 0.0.1
2. Clears version-specific NuGet global-packages cache
3. Clears NuGet HTTP metadata cache
4. Deletes all `bin/` and `obj/` folders in Samples
5. Restores packages with `--no-cache --force`
6. Builds all sample projects

**Why comprehensive cache clearing is critical**:
- NuGet caches package metadata in HTTP cache
- MSBuild caches resolved assemblies
- Source generators cache outputs in `obj/`
- Without clearing, old SDK versions may be used

#### 4. Verify Build Outputs

After rebuilding samples, check:
- Source generator outputs in `obj/` directories
- Deployed mod files in game directory
- Build logs for warnings/errors
- Localization CSV validation results

### Local Development Setup

#### Prerequisites

1. **.NET SDK 9.0** (specified in `Sdk/global.json`)
2. **dotnet-script** (for running `.csx` automation scripts)
   ```bash
   dotnet tool install -g dotnet-script
   ```
3. **ilrepack** (optional, for assembly merging)
   ```bash
   dotnet tool install -g dotnet-ilrepack
   ```

#### Game Dependencies Setup

Game assemblies are not in the repository. Download them once:

```bash
./scripts/fetch_build_dependency.sh
```

**What this script does**:
- Downloads game DLLs from `sdk_build_dependency` GitHub release
- Extracts to `./Managed/` directory
- Includes Unity, TeamSoda, and game-specific assemblies
- Required for building SDK and samples

#### Local Configuration

Test projects require `Local.props` (git-ignored) to specify game location:

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

### Testing Strategy

#### Unit Tests
- Located in `Sdk/Tests/Ducky.Sdk.Lib.Tests/`
- Test SDK library components in isolation
- Run with: `dotnet test Sdk/Tests/`

#### Integration Tests (via Samples)
- Sample projects in `Samples/` serve as integration tests
- **Purpose**: Verify SDK works end-to-end in real mod projects
- **Not production code**: Samples are temporary verification projects
- Test different SDK features:
  - Single-project mods
  - Multi-project mods with shared libraries
  - Harmony integration
  - Localization system
  - Inter-mod communication patterns

#### End-to-End Validation Workflow

```bash
# 1. Make SDK changes
# 2. Package SDK
./scripts/packToLocal.sh --version 0.0.1

# 3. Test with samples
./scripts/rebuild_samples.sh

# 4. Verify outputs
ls -la ~/.local/share/Steam/steamapps/common/Escape\ from\ Duckov/Mods/

# 5. Check build logs for errors
# 6. Test in-game if needed
```

### Debugging SDK Issues

**Source Generator not running**:
- Check `obj/` directory for generated files
- Enable diagnostic output: `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>`
- View generated files in `obj/generated/`

**MSBuild targets not executing**:
- Add diagnostic logging: `dotnet build -v:detailed`
- Check target execution order in build log
- Verify `.props` and `.targets` are imported

**NuGet cache issues**:
- Clear all caches: `dotnet nuget locals all --clear`
- Delete specific package: `rm -rf ~/.nuget/packages/ducky.sdk/`
- Use `--purge-all-versions` flag with `packToLocal.sh`

**Script execution failures**:
- Ensure dotnet-script is installed
- Check script syntax: `dotnet script scripts/scriptname.csx --help`
- View script output in build logs

## Mod Development Conventions

### ModBehaviour Pattern

Every mod has a class inheriting `ModBehaviourBase` (provided by the SDK via `Ducky.Sdk.Lib`):

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

**Note**: `ModBehaviourBase` is distributed as source code via the SDK's `contentFiles` mechanism, allowing you to see and debug the implementation directly in your project.

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

**Harmony support** (optional runtime patching):
```xml
<PropertyGroup>
  <ModName>Ducky.MyHarmonyMod</ModName>
  <IncludeHarmony>true</IncludeHarmony> <!-- Adds 0Harmony.dll reference -->
</PropertyGroup>
```
When `IncludeHarmony=true`:
- 0Harmony.dll is referenced from SDK's `tools/` directory
- All dependencies are automatically merged into the final mod DLL via ILRepack
- No need to manually manage Harmony installation or deployment

### Build Outputs and Deployment

On build, the SDK's `.targets` file executes these steps in order:

1. **EnsureInfoIni** - Generates basic `info.ini` if missing (with name, displayName, description)
2. **GeneratePreview** - Creates 256x256 identicon-style `preview.png` based on mod name hash (if missing)
3. **CollectFromMod** - Syncs `publishedFileId` from deployed mod back to source `info.ini` (for Steam Workshop)
4. **CopyToDuckov** - Cleans and deploys to game directory:
   - **Deletes** entire mod folder to ensure clean state
   - Deploys assets/ folder (info.ini, description.md, preview.png, Locales/)
   - Copies final mod DLL
5. **CopyMissingDependencies** - (Only if `EnableILRepack=false`) Copies non-game DLLs to `Dependency/` subfolder
6. **PackModWithILRepack** - (Default, `EnableILRepack=true`) Merges all assemblies into single DLL:
   - Creates temporary folder `.ilrepack_temp/` for merging
   - Merges primary DLL + all NuGet dependencies (excluding game assemblies)
   - Copies merged DLL to final location with clean assembly name
   - Removes temporary folder

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

## Advanced Features

### Harmony Runtime Patching

The SDK provides optional Harmony support for runtime IL manipulation and method patching:

**Enabling Harmony**:
```xml
<PropertyGroup>
  <IncludeHarmony>true</IncludeHarmony>
</PropertyGroup>
```

**What happens when enabled**:
1. SDK adds a reference to `0Harmony.dll` from the NuGet package's `tools/` directory
2. All Harmony dependencies are automatically discovered and copied to output
3. During deployment, ILRepack merges 0Harmony.dll and all dependencies into the final mod DLL
4. The merged assembly has a clean name without any `.packed.tmp` suffixes

**Example Harmony usage**:
```csharp
using HarmonyLib;

public class ModBehaviour : ModBehaviourBase
{
    private Harmony _harmony;
    
    protected override void ModEnabled()
    {
        _harmony = new Harmony("com.yourname.modname");
        _harmony.PatchAll(); // Apply all [HarmonyPatch] attributes
    }
    
    protected override void ModDisabled()
    {
        _harmony?.UnpatchAll();
    }
}

[HarmonyPatch(typeof(SomeGameClass), nameof(SomeGameClass.SomeMethod))]
public static class SomeGameClass_SomeMethod_Patch
{
    static void Prefix(/* method parameters */)
    {
        // Code runs before original method
    }
}
```

**Harmony benefits**:
- No game source code modifications needed
- Patches are applied at runtime
- Multiple mods can patch the same methods
- Easy to enable/disable via mod loader

### Assembly Merging with ILRepack

The SDK uses ILRepack to merge all mod assemblies into a single DLL for easier distribution and reduced mod conflicts.

**How it works**:
1. **Dependency Collection** (`CollectModDependencies` target):
   - Scans output directory for all DLL files
   - Excludes game assemblies from `Managed/` directory
   - Identifies NuGet dependencies via `CopyLocalLockFileAssemblies=true`

2. **Library Path Resolution**:
   - Project output directory (all copied dependencies)
   - Game's Managed directory (Unity, TeamSoda assemblies)
   - All reference assembly directories (from `@(ReferencePath)`)
   - NuGet runtime assemblies (from `@(RuntimeCopyLocalItems)`)

3. **Merging Process** (`PackModWithILRepack` target):
   - Creates temporary folder `.ilrepack_temp/` in mod directory
   - Runs ILRepack with `/internalize` to make dependencies internal
   - Output: `ModName.dll` in temp folder (clean assembly name)
   - Copies merged DLL to final location
   - Deletes temporary folder

4. **Clean Deployment** (`CopyToDuckov` target):
   - **Deletes entire mod folder** before each deployment
   - Ensures no stale files or old dependencies
   - Copies only: merged DLL, assets, localization files

**Configuration**:
```xml
<PropertyGroup>
  <!-- Enable/disable ILRepack (default: true) -->
  <EnableILRepack>true</EnableILRepack>
  
  <!-- When false, copies dependencies to Dependency/ subfolder instead -->
  <EnableILRepack>false</EnableILRepack>
</PropertyGroup>
```

**Benefits**:
- Single DLL distribution (easier to manage)
- No dependency version conflicts between mods
- Internalized dependencies (no namespace pollution)
- Smaller mod footprint (no separate dependency files)

**Typical merged assembly size**:
- Without dependencies: ~100KB
- With Harmony + R3 + Serilog: ~4.2MB
- All dependencies are properly embedded and internalized

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
- **$(EnableILRepack)** - Enable assembly merging (default: `true`)
- **$(IncludeHarmony)** - Enable Harmony support (default: `false`)
- **$(CopyLocalLockFileAssemblies)** - Copy NuGet dependencies to output (default: `true`, set in SDK)
- **$(AssetsDir)** - Custom assets directory (defaults to `assets/`)

### ContentFiles Visibility
SDK sources are hidden from Solution Explorer:
```xml
<Compile Update="@(_DuckySdkCompileForVisibility)">
  <Visible>false</Visible>
</Compile>
```

## SDK Testing and Validation

See **Critical Development Workflows > Testing Strategy** section above for complete testing procedures.

**Quick Reference**:
- Unit tests: `dotnet test Sdk/Tests/`
- Integration tests: `./scripts/rebuild_samples.sh`
- Samples are for SDK validation only, not production templates

## Package Versioning

- **Development**: Always use `0.0.1` for samples (hardcoded in `rebuild_samples.sh`)
- **Production**: Use `--version x.y.z` with `packToLocal.sh` or rely on MinVer in CI (`Directory.Packages.props` includes MinVer when `$(CI) == 'true'`)

## CI/CD and Publishing

### GitHub Actions Workflows

The repository uses GitHub Actions for automated builds and releases:

1. **build_dotnet.yml** - Continuous integration build
   - Triggers on push and pull requests
   - Validates SDK builds successfully
   - Runs unit tests
   - Builds all sample projects

2. **dotnet_format.yml** - Code formatting validation
   - Ensures consistent code style
   - Runs `dotnet format` verification
   - Fails if formatting issues detected

3. **publish.yml** - NuGet package publishing
   - **Triggers**: 
     - On tags matching `*.*.*` pattern (formal releases)
     - On pushes to `main` branch (dev builds)
   - **Version Strategy**:
     - **Tag builds**: Uses tag name as version (e.g., `1.2.3`)
     - **Main branch**: Uses GitVersion with format `X.Y.Z-dev.N` where N is commit count
   - **Steps**:
     1. Fetches game dependencies via `fetch_build_dependency.sh`
     2. Determines version from tag or GitVersion
     3. Runs `packToLocal.sh` with Release configuration
     4. Uploads artifacts to GitHub
     5. Publishes to NuGet.org (using `NUGET_KEY` secret)
   - **Outputs**: Both `.nupkg` and `.snupkg` (symbol package)

4. **release-drafter.yml** - Automated release notes
   - Drafts release notes from PR titles
   - Categorizes changes by labels
   - Updates on every push to main

### Publishing Workflow

**For formal releases**:
```bash
# 1. Ensure all changes are committed
git tag 1.2.3
git push origin 1.2.3

# 2. GitHub Actions automatically:
#    - Builds with version 1.2.3
#    - Publishes to NuGet.org
#    - Creates GitHub release draft
```

**For development builds**:
```bash
# Push to main branch
git push origin main

# GitHub Actions automatically:
# - Builds with version like 1.2.3-dev.42
# - Publishes to NuGet.org as pre-release
```

### Version Management

- **GitVersion.yml** - Controls version calculation
- **global.json** - Pins .NET SDK version (9.0)
- **Directory.Packages.props** - Central package version management
  - Includes MinVer when `$(CI) == 'true'`
  - Manages all NuGet dependencies centrally

### Local Testing Before Release

```bash
# 1. Test packaging locally
./scripts/packToLocal.sh --version 1.2.3-test --configuration Release

# 2. Verify all samples build with new package
./scripts/rebuild_samples.sh

# 3. Check package contents
unzip -l duckylocal/Ducky.Sdk.1.2.3-test.nupkg
```

## Common Pitfalls

1. **"SteamDir property must be set"** - Create `Samples/Local.props` with `<SteamFolder>`
2. **Stale NuGet cache after SDK changes** - Always use `./scripts/rebuild_samples.sh` which clears:
   - Version-specific global-packages cache
   - HTTP metadata cache (ensures fresh package info)
   - All bin/obj directories
3. **Missing game assemblies** - Run `fetch_build_dependency.sh` before building
4. **Localization keys missing in CSV** - Generator creates `LKeys.All` array; MSBuild task validates CSV completeness
5. **Mod not deploying** - Check `$(DeployMod)` property and `$(DuckovFolder)` path validity
6. **Preview.png not generating** - Ensure `info.ini` exists with `name` field; check build logs for script errors
7. **ILRepack not merging** - Ensure ilrepack is installed globally: `dotnet tool install -g dotnet-ilrepack`
8. **Dependencies not being merged** - Ensure `CopyLocalLockFileAssemblies=true` is set (SDK sets this by default)
9. **Old SDK cached during development** - Use `--purge-all-versions` flag or manually delete `~/.nuget/packages/ducky.sdk/`
10. **Assembly name contains .packed.tmp** - Fixed in current version; SDK uses temporary folder approach

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

## SDK Development Best Practices

### When Modifying SDK Components

#### 1. Analyser (Source Generator) Changes
**Location**: `Sdk/Ducky.Sdk.Analyser/`  
**Language**: C# (.NET Standard 2.0)

**Workflow**:
```bash
# 1. Edit source generator code
# 2. Build the analyzer project
dotnet build Sdk/Ducky.Sdk.Analyser/

# 3. Package SDK (copies analyzer DLL automatically)
./scripts/packToLocal.sh --version 0.0.1

# 4. Test with samples
./scripts/rebuild_samples.sh
```

**Key Points**:
- Must target .NET Standard 2.0 (Roslyn requirement)
- Implement `IIncrementalGenerator` for performance
- Test with `EmitCompilerGeneratedFiles=true` to view outputs
- Check generated files in `obj/generated/` directory

#### 2. Library (Shared Code) Changes
**Location**: `Sdk/SDKlibs/Ducky.Sdk.Lib/`  
**Distribution**: Source files via `contentFiles/`

**Workflow**:
```bash
# 1. Edit .cs files directly
# 2. Package SDK (no build needed - source distribution)
./scripts/packToLocal.sh --version 0.0.1

# 3. Test with samples
./scripts/rebuild_samples.sh
```

**Key Points**:
- Changes are distributed as source code
- No compilation needed for SDK itself
- Mod projects compile these files
- Test changes immediately after packaging

#### 3. Build Logic Changes
**Location**: `Sdk/SDKlibs/Ducky.Sdk/`  
**Files**: `.props`, `.targets`, `.csx` scripts

**Workflow for .props/.targets**:
```bash
# 1. Edit Ducky.Sdk.props or Ducky.Sdk.targets
# 2. Package SDK
./scripts/packToLocal.sh --version 0.0.1

# 3. Test with verbose logging
cd Samples/Ducky.SingleProject
dotnet build -v:detailed
```

**Workflow for .csx scripts**:
```bash
# 1. Edit script in Sdk/SDKlibs/Ducky.Sdk/scripts/
# 2. Test script standalone (optional)
dotnet script Sdk/SDKlibs/Ducky.Sdk/scripts/scriptname.csx

# 3. Package SDK
./scripts/packToLocal.sh --version 0.0.1

# 4. Test in build pipeline
./scripts/rebuild_samples.sh
```

**Key Points**:
- `.props` files define properties and references
- `.targets` files define build tasks and execution order
- `.csx` scripts are pure C# (dotnet-script format)
- Use `dotnet build -v:detailed` to debug MSBuild issues

### Testing Workflow

**Complete SDK validation**:
```bash
# 1. Make changes to any SDK component
# 2. Run comprehensive test
./scripts/rebuild_samples.sh

# This automatically:
# - Packages SDK with version 0.0.1
# - Clears all NuGet caches
# - Rebuilds all sample projects
# - Validates end-to-end workflow
```

**Quick iteration** (when caches are clean):
```bash
# 1. Package SDK
./scripts/packToLocal.sh --version 0.0.1 --no-clear-cache

# 2. Build specific sample
cd Samples/Ducky.SingleProject
dotnet build --no-restore
```

### Pre-Commit Checklist

Before committing SDK changes:

- [ ] Run `./scripts/rebuild_samples.sh` successfully
- [ ] Check for build warnings in output
- [ ] Verify generated files in sample `obj/` directories
- [ ] Test deployed mods in game directory
- [ ] Update AGENTS.md if architecture changed
- [ ] Update README.md if user-facing features changed
- [ ] Run unit tests: `dotnet test Sdk/Tests/`

### Common Modification Scenarios

**Adding a new MSBuild property**:
1. Define in `Ducky.Sdk.props` with default value
2. Document in AGENTS.md under "MSBuild Properties"
3. Use in `.targets` file if needed
4. Test with sample project

**Adding a new source generator feature**:
1. Modify `DuckyLocalizationGenerator.cs`
2. Update generated file templates
3. Test incremental generation behavior
4. Verify performance with large projects

**Adding a new automation script**:
1. Create `.csx` file in `Sdk/SDKlibs/Ducky.Sdk/scripts/`
2. Add to `.nuspec` file for packaging
3. Call from `.targets` file via `Exec` task
4. Test script execution in build pipeline

**Adding a new library class**:
1. Create `.cs` file in `Sdk/SDKlibs/Ducky.Sdk.Lib/`
2. Ensure proper namespace (`Ducky.Sdk.*`)
3. Add XML documentation comments
4. File is automatically included via `contentFiles/`

## External Dependencies

- **Game**: "Escape from Duckov" (Unity game, references via `Managed/` folder)
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
