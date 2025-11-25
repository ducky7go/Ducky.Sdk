## ADDED Requirements

### Requirement: Dedicated LocalizationAssetsDir Property
The SDK SHALL provide a dedicated `LocalizationAssetsDir` MSBuild property for specifying localization asset directories separate from the general `AssetsDir` property.

#### Scenario: Primary usage with LocalizationAssetsDir
- **WHEN** developer sets `LocalizationAssetsDir` property in project file
- **THEN** UpdateLocalesCsv target SHALL use only `LocalizationAssetsDir` for localization assets
- **AND** other targets SHALL continue using `AssetsDir` for general assets

#### Scenario: Backward compatibility fallback
- **WHEN** developer does not set `LocalizationAssetsDir` property
- **THEN** UpdateLocalesCsv target SHALL fall back to using `AssetsDir` property
- **AND** existing projects SHALL continue to work without changes

#### Scenario: Multiple localization directories
- **WHEN** developer sets `LocalizationAssetsDir` with semicolon-separated paths
- **THEN** UpdateLocalesCsv target SHALL process all specified directories
- **AND** SHALL generate localization files for each directory

#### Scenario: Single localization directory
- **WHEN** developer sets `LocalizationAssetsDir` to a single path
- **THEN** UpdateLocalesCsv target SHALL process only that directory
- **AND** SHALL treat it as a single directory path

## MODIFIED Requirements

### Requirement: UpdateLocalesCsv Target Asset Resolution
The UpdateLocalesCsv target SHALL resolve localization asset directories using `LocalizationAssetsDir` as the primary property, with `AssetsDir` as fallback when `LocalizationAssetsDir` is not specified.

#### Scenario: Property precedence
- **WHEN** both `LocalizationAssetsDir` and `AssetsDir` are specified
- **THEN** UpdateLocalesCsv target SHALL prioritize `LocalizationAssetsDir` over `AssetsDir`
- **AND** SHALL log which property is being used for transparency

#### Scenario: Logging for debugging
- **WHEN** UpdateLocalesCsv target executes
- **THEN** target SHALL log the resolved property being used
- **AND** SHALL show absolute paths of all directories being processed