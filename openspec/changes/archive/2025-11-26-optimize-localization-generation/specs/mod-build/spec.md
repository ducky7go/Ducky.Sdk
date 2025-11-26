# mod-build Specification

## Purpose

Defines the build system capabilities for mod development including automatic generation and synchronization of mod metadata files, particularly info.ini files with proper name field handling.

## ADDED Requirements

### Requirement: LocalizationAssetsDir-Based Asset Generation Optimization
The build system SHALL optimize multi-language asset generation by using LocalizationAssetsDir-based centralized generation and asset distribution.

#### Scenario: LocalizationAssetsDir Multi-Directory Optimization
- **WHEN** `LocalizationAssetsDir` contains multiple directory paths
- **THEN** the system SHALL generate localization assets once in the project's local `assets/` folder
- **AND** SHALL copy all generated assets to each directory specified in `LocalizationAssetsDir`
- **AND** SHALL maintain identical file sets across all target directories

#### Scenario: Single Directory Behavior Preservation
- **WHEN** `LocalizationAssetsDir` contains only one directory path or is empty
- **THEN** the system SHALL generate assets directly in the target directory without copying
- **AND** SHALL behave identically to the previous implementation

### Requirement: Asset Copy Distribution System
The build system SHALL implement reliable copying of generated localization assets from the primary location to all LocalizationAssetsDir targets.

#### Scenario: Complete Asset Distribution
- **WHEN** localization assets are generated in the primary location
- **THEN** the system SHALL copy all asset types to each target directory:
  - `lkeys.json` files to the target assets root
  - CSV files to the target `Locales/` subdirectory
  - Language subdirectories (`en/`, `zh/`, etc.) with all content files
  - `keys.hash.txt` files to the target `Locales/` subdirectory
- **AND** SHALL preserve complete directory structure

#### Scenario: Reliable File Copying
- **WHEN** copying assets between directories
- **THEN** the system SHALL use system commands (`cp`, `rsync`) for reliable operations
- **AND** SHALL create target directories as needed
- **AND** SHALL handle file permissions and timestamps correctly

### Requirement: UpdateLocalesCsv Target Enhancement
The `UpdateLocalesCsv` target SHALL be enhanced to support centralized generation and distribution workflow.

#### Scenario: Centralized Generation Execution
- **WHEN** the `UpdateLocalesCsv` target executes with multiple LocalizationAssetsDir entries
- **THEN** it SHALL perform centralized generation in the project's local assets folder
- **AND** SHALL invoke asset copying to all LocalizationAssetsDir targets
- **AND** SHALL perform consistency validation before completion

#### Scenario: Build Performance Improvement
- **WHEN** building projects with multiple LocalizationAssetsDir directories
- **THEN** the `UpdateLocalesCsv` target SHALL complete faster than the previous implementation
- **AND** SHALL reduce overall build time by eliminating redundant generation

### Requirement: ExtractLKeysJson Target Enhancement
The `ExtractLKeysJson` target SHALL be enhanced to support centralized generation.

#### Scenario: JSON File Centralization
- **WHEN** `LocalizationAssetsDir` contains multiple directory paths
- **THEN** the `ExtractLKeysJson` target SHALL generate `lkeys.json` files only in the primary location
- **AND** SHALL copy generated JSON files to all LocalizationAssetsDir targets
- **AND** SHALL ensure consistent JSON content across all directories

### Requirement: Conditional Game Dependencies Import
The build system SHALL conditionally import game-specific namespaces based on dependency availability.

#### Scenario: Game Dependency Detection
- **WHEN** a project references Ducky.Sdk with global usings enabled
- **THEN** the system SHALL include TeamSoda, FOW, and SodaLocalization only when corresponding DLL files exist
- **AND** SHALL prevent compilation errors when game dependencies are missing
- **AND** SHALL maintain full functionality when all dependencies are available