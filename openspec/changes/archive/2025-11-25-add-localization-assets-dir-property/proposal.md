# Change: Add dedicated LocalizationAssetsDir property for UpdateLocalesCsv target

## Why
The `UpdateLocalesCsv` target currently uses `AssetsDir` property which supports multiple directories (semicolon-separated), but other targets expect `AssetsDir` to be a single path. This creates architectural inconsistency and potential conflicts when different targets have different expectations for the same property.

## What Changes
- Add new `LocalizationAssetsDir` MSBuild property specifically for localization assets
- Modify `UpdateLocalesCsv` target to use `LocalizationAssetsDir` as primary source
- Maintain backward compatibility by falling back to `AssetsDir` when `LocalizationAssetsDir` is not specified
- Update documentation to clarify the distinction between these properties

## Impact
- Affected specs: MSBuild integration, Localization system
- Affected code: `Ducky.Sdk.targets` file, UpdateLocalesCsv target
- **BREAKING**: None (backward compatible)
- Migration: Optional - existing projects continue to work, new projects can use dedicated property for clarity