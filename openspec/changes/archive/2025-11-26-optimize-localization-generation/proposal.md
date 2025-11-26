# Optimize Multi-Language Asset Generation

## Problem Statement

Currently, when multiple localization directories are specified using `LocalizationAssetsDir`, the system generates CSV files and translation assets separately for each directory. This approach:

- Creates redundant translation work
- Leads to potential inconsistencies between directories
- Increases build time unnecessarily
- Wastes storage space with duplicate assets

## Why

The current implementation treats each directory in `LocalizationAssetsDir` independently, running the full localization generation process separately for each one. This is inefficient because:

1. **Redundant Work**: The same localization keys and translations are processed multiple times
2. **Inconsistency Risk**: Separate generation can lead to different file versions across directories
3. **Performance Impact**: Build times increase linearly with the number of directories
4. **Storage Waste**: Duplicate translation files consume unnecessary disk space

## What Changes

### Implementation Changes:
1. **Centralized Generation**: Generate all localization assets once in the project's local `assets/` folder when `LocalizationAssetsDir` is specified
2. **Asset Distribution**: Copy generated assets to all directories listed in `LocalizationAssetsDir`
3. **Simplified Logic**: Remove dependency on `IsModLib` property - work purely based on `LocalizationAssetsDir` presence
4. **System Commands**: Use reliable Unix commands (`cp`, `rsync`) for file copying operations

### Target Changes:
- **ExtractLKeysJson**: Generate `lkeys.json` only in primary location, then copy to targets
- **UpdateLocalesCsv**: Generate CSV and translation files once in primary location, then distribute
- **New Target**: `SimpleCopyLocalizationAssets` for reliable file copying
- **New Target**: `CopyLocalizationAssets` for MSBuild-based copying (legacy)

## Goals

- Eliminate redundant CSV generation across multiple localization directories
- Reduce build time for multi-directory localization setups
- Maintain consistency across all localization directories
- Preserve existing `LocalizationAssetsDir` API compatibility
- Improve developer experience for complex multi-project mods