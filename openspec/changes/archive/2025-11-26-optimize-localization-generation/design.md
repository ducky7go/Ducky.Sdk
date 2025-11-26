# Localization Generation Optimization Design

## Architecture Overview

### Current Implementation
```
LocalizationAssetsDir: "assets/Locales;shared-locales;external-translations"

Current Process:
1. Generate CSV files in assets/Locales/
2. Generate CSV files in shared-locales/
3. Generate CSV files in external-translations/
4. Process each directory independently
```

### Proposed Implementation
```
LocalizationAssetsDir: "assets/Locales;shared-locales;external-translations"

New Process:
1. Identify primary generation location (IsModLib or first directory)
2. Generate CSV files and translation assets once in primary location
3. Copy generated assets to additional directories
4. Validate consistency across all directories
```

## Key Components

### 1. Primary Location Detection
- Priority 1: `IsModLib=true` project's localization directory
- Priority 2: First directory in `LocalizationAssetsDir` list
- Fallback: Default `assets/Locales` location

### 2. Asset Generation Strategy
- Generate CSV files only in primary location
- Generate translation files (Markdown, etc.) only in primary location
- Ensure all localization keys are processed once

### 3. Asset Copy Mechanism
- Copy generated CSV files to all additional directories
- Copy generated translation files to corresponding language subdirectories
- Preserve file permissions and timestamps

### 4. Validation and Consistency
- Verify all directories have identical file sets
- Check for missing or extra files in target directories
- Ensure proper directory structure is maintained

## Implementation Details

### MSBuild Target Changes
- Modify `UpdateLocalesCsv` target to support centralized generation
- Add new `CopyLocalizationAssets` target for distribution
- Maintain backward compatibility with single-directory setups

### File Structure Management
- Handle nested directory structures properly
- Preserve language-specific subdirectory organization
- Support mixed file types (CSV, MD, TXT, etc.)

### Error Handling
- Graceful handling of copy failures
- Validation of successful asset distribution
- Clear error messages for configuration issues