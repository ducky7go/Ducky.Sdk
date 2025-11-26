## MODIFIED Requirements
### Requirement: LocalizationAssetsDir Multi-Path Formatting
The LocalizationAssetsDir property SHALL use clean semicolon-separated path formatting without spaces after semicolons.

#### Scenario: Multiple directory configuration
- **WHEN** LocalizationAssetsDir contains multiple paths for localization assets
- **THEN** paths SHALL be separated by semicolons without spaces: `path1;path2;path3`
- **AND** each path SHALL be properly parsed by MSBuild without empty string entries
- **AND** the build system SHALL correctly resolve each directory for localization processing

#### Scenario: MSBuild path combination with multiple directories
- **WHEN** using MSBuild path combination functions to construct multiple localization paths
- **THEN** the final LocalizationAssetsDir value SHALL use clean semicolon separators
- **AND** SHALL not contain spaces that create empty string artifacts in MSBuild parsing