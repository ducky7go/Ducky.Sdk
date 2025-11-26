## MODIFIED Requirements

### Requirement: INI File Name Synchronization
The build system SHALL automatically synchronize the name field in info.ini files with the ModName property from MSBuild configuration using a centralized property resolution target.

#### Scenario: Centralized Property Resolution for INI Synchronization
- **WHEN** the `ResolveDuckySdkProperties` target executes before `EnsureInfoIni`
- **THEN** the system SHALL determine if INI synchronization should occur based on `IsModLib`, `DeployMod`, and `ModName` properties
- **AND** SHALL pass this decision to `EnsureInfoIni` without recalculating the same logic

#### Scenario: Early Validation for Configuration Conflicts
- **WHEN** the `ResolveDuckySdkProperties` target detects conflicting configurations (e.g., `IsModLib=true` with `DeployMod=true`)
- **THEN** the system SHALL emit clear warning messages explaining the conflict resolution
- **AND** SHALL proceed with the most restrictive setting

### Requirement: Global Using Directives
The build system SHALL automatically provide global using directives for essential SDK, game engine, and third-party namespaces when Ducky.Sdk is referenced, with centralized property management for conditional imports.

#### Scenario: Conditional Game Engine Import Optimization
- **WHEN** the `ResolveDuckySdkProperties` target checks for game dependency availability
- **THEN** it SHALL set `_HasGameDependencies` property based on actual DLL existence
- **AND** SHALL prevent multiple targets from performing the same file system checks
- **AND** SHALL cache the result for the duration of the build

#### Scenario: Centralized Global Using Configuration
- **WHEN** `EnableGlobalUsing` property is resolved in `ResolveDuckySdkProperties`
- **THEN** all subsequent targets SHALL use the resolved value without re-evaluation
- **AND** global using ItemGroups SHALL be conditionally included based on the centralized decision

### Requirement: LocalizationAssetsDir-Based Asset Generation Optimization
The build system SHALL optimize multi-language asset generation by using LocalizationAssetsDir-based centralized generation and asset distribution with intelligent caching and change detection.

#### Scenario: Centralized Localization Property Resolution
- **WHEN** the `ResolveDuckySdkProperties` target processes localization configuration
- **THEN** it SHALL resolve `_EffectiveLocalizationAssetsDir`, `_PrimaryLocalizationDir`, and `_HasMultipleLocalizationDirs` properties once
- **AND** SHALL make these properties available to all localization targets without recalculation

#### Scenario: Smart Cache-Based Key Extraction
- **WHEN** the `ExtractLKeysJson` target executes with caching enabled
- **THEN** it SHALL compare generated file timestamps with cache metadata
- **AND** SHALL skip key extraction if no source files have changed since last successful extraction
- **AND** SHALL use cached JSON files for subsequent localization steps

#### Scenario: Change-Based Asset Copying
- **WHEN** the `CopyLocalizationAssets` target processes multiple directories
- **THEN** it SHALL compare source and target file timestamps before copying
- **AND** SHALL only copy files that have changed or are missing in target directories
- **AND** SHALL preserve file permissions and attributes during copying

### Requirement: Build Target Integration
The MSBuild targets SHALL integrate all optimization features into the existing build pipeline with clear phase separation and dependency management.

#### Scenario: Phased Target Execution
- **WHEN** the build process starts
- **THEN** the system SHALL execute targets in the following order:
  1. `ValidateProjectPath` and `ValidateDuckovFolder` (Validation Phase)
  2. `ResolveDuckySdkProperties` (Property Resolution Phase)
  3. `EnsureInfoIni` and `GeneratePreview` (Asset Generation Phase)
  4. `ExtractLKeysJson` and `UpdateLocalesCsv` (Localization Phase)
  5. `CopyToDuckov` and `PackModWithILRepack` (Packaging Phase)
- **AND** each phase SHALL complete successfully before the next phase begins

#### Scenario: Target Dependency Optimization
- **WHEN** multiple targets need the same property or file system information
- **THEN** the first target SHALL calculate the value and store it in a shared property
- **AND** subsequent targets SHALL use the stored value without recalculation
- **AND** SHALL skip execution entirely if preconditions are not met

### Requirement: Conditional Game Dependencies Import
The build system SHALL conditionally import game-specific namespaces based on dependency availability using centralized detection logic.

#### Scenario: Centralized Game Dependency Detection
- **WHEN** the `ResolveDuckySdkProperties` target runs
- **THEN** it SHALL perform a single check for game DLL availability in ManagedDirectory
- **AND** SHALL set `_HasTeamSoda`, `_HasUnity`, `_HasFOW`, and `_HasSodaLocalization` properties
- **AND** SHALL make these properties available to all targets without rechecking file system

#### Scenario: Optimized Reference Addition
- **WHEN** the `AddDuckyManagedReferences` target executes
- **THEN** it SHALL use the pre-calculated game dependency properties instead of performing its own file system checks
- **AND** SHALL only attempt to add references for dependencies that are actually available
- **AND** SHALL provide clear warnings for missing game dependencies