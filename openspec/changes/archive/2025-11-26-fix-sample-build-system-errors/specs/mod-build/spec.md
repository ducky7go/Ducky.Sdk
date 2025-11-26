## MODIFIED Requirements
### Requirement: Build Target Execution Order
The build system SHALL execute localization targets in the correct sequence to ensure proper file generation.

#### Scenario: Localization key extraction
- **WHEN** building projects with LK.cs files containing LanguageSupport attributes
- **THEN** ExtractLocalizationKeysCSX target runs AFTER compilation completes
- **AND** ExtractLocalizationKeysCSX target runs BEFORE UpdateLocalizationCsvCSX target
- **AND** generated lkeys.json contains keys extracted from compiled assembly

#### Scenario: Validation script execution
- **WHEN** build system runs validation scripts in MSBuild context
- **THEN** scripts execute with the same working directory as manual execution
- **AND** script arguments are passed correctly in build environment
- **AND** exit codes are correctly interpreted by MSBuild

#### Scenario: Script parameter resolution
- **WHEN** CSX scripts require build properties
- **THEN** LocalizationAssetsDir parameter is correctly resolved and passed
- **AND** AssetsDir parameter uses project-specific values when available
- **AND** BuildContext receives all required parameters for script execution

## ADDED Requirements
### Requirement: Build Sequence Dependency Management
The build system SHALL manage target dependencies to ensure proper execution order.

#### Scenario: Localization processing pipeline
- **WHEN** ProcessLocalization target is executed
- **THEN** compilation phase completes successfully first
- **AND** ExtractLocalizationKeysCSX generates lkeys.json from compiled output
- **AND** UpdateLocalizationCsvCSX uses the generated lkeys.json file
- **AND** CopyLocalizationAssetsCSX copies generated files to correct locations

### Requirement: Auto-Generation Workflow Validation
The build system SHALL ensure lkeys.json files are auto-generated from LK.cs rather than manually created.

#### Scenario: LK.cs with LanguageSupport attribute
- **WHEN** project contains LK.cs with LanguageSupport("en", "zh") attributes
- **THEN** ExtractLocalizationKeysCSX extracts these language codes
- **AND** generates lkeys.json with supportedLanguages array containing "en", "zh"
- **AND** creates entries for all const string values in LK class hierarchy
- **AND** places lkeys.json in the correct assets directory for CSV generation

### Requirement: DeployMod Cleanup Behavior
The build system SHALL clean mod files from game directory when deployment is disabled rather than skipping cleanup entirely.

#### Scenario: DeployMod=false configuration
- **WHEN** DeployMod property is set to false
- **THEN** deployment scripts SHALL NOT skip execution entirely
- **AND** CleanTargetDirectory SHALL be called to remove existing mod files
- **AND** no new files SHALL be copied to game directory
- **AND** build SHALL succeed without deployment errors

#### Scenario: Deployment mode toggle
- **WHEN** developer switches between DeployMod=true and DeployMod=false
- **THEN** game directory SHALL be properly cleaned of mod artifacts
- **AND** no stale or orphaned mod files SHALL remain
- **AND** subsequent deployments SHALL work correctly