## ADDED Requirements

### Requirement: IsModLib Conditional Task Execution
The build system SHALL conditionally execute automation tasks based on the `IsModLib` property setting.

#### Scenario: Skip non-essential tasks for library projects
- **WHEN** a project has `IsModLib=true`
- **THEN** the system SHALL skip preview PNG generation
- **AND** the system SHALL skip mod deployment to game directory
- **AND** the system SHALL skip ILRepack assembly merging
- **AND** the system SHALL skip dependency file copying
- **AND** the system SHALL log which tasks are skipped due to IsModLib flag

#### Scenario: Continue essential tasks for library projects
- **WHEN** a project has `IsModLib=true`
- **THEN** the system SHALL process localization strings and CSV updates
- **AND** the system SHALL extract localization keys to JSON
- **AND** the system SHALL skip info.ini metadata generation

### Requirement: Modular Build File Organization
The build system SHALL organize MSBuild targets into separate focused files for better maintainability.

#### Scenario: File structure separation
- **WHEN** the SDK is structured
- **THEN** Ducky.Sdk.Validation.targets SHALL contain path validation and folder checks
- **AND** Ducky.Sdk.Localization.targets SHALL contain localization processing tasks
- **AND** Ducky.Sdk.Assets.targets SHALL contain asset generation and copying tasks
- **AND** Ducky.Sdk.Packaging.targets SHALL contain ILRepack and deployment tasks
- **AND** the main Ducky.Sdk.targets SHALL import the modular files in correct order

#### Scenario: Property organization
- **WHEN** properties are defined in Ducky.Sdk.props
- **THEN** IsModLib-related logic SHALL be grouped in a dedicated section
- **AND** conditional properties SHALL be clearly documented with IsModLib conditions

## MODIFIED Requirements

### Requirement: Automated Mod Asset Generation
The build system SHALL automatically generate required mod assets during build process for final release mod projects.

#### Scenario: Skip asset generation for library projects
- **WHEN** project has `IsModLib=true`
- **THEN** preview.png generation SHALL be skipped
- **AND** a message SHALL be logged indicating skipped task
- **WHEN** project has `IsModLib=false` or property is not set
- **THEN** preview.png generation SHALL proceed normally

### Requirement: Mod Deployment Pipeline
The build system SHALL deploy compiled mods to the Duckov mods directory with all required files and dependencies.

#### Scenario: Skip deployment for library projects
- **WHEN** project has `IsModLib=true`
- **THEN** mod deployment to game directory SHALL be skipped
- **AND** dependency copying SHALL be skipped
- **AND** a message SHALL be logged indicating deployment skipped
- **WHEN** project has `IsModLib=false` or property is not set
- **THEN** full deployment pipeline SHALL execute normally

### Requirement: Assembly Merging with ILRepack
The build system SHALL merge the main mod assembly with its dependencies into a single DLL when ILRepack is enabled.

#### Scenario: Skip ILRepack for library projects
- **WHEN** project has `IsModLib=true`
- **THEN** ILRepack merging SHALL be skipped regardless of EnableILRepack setting
- **AND** original assembly output SHALL be preserved without merging
- **AND** a message SHALL be logged indicating ILRepack skipped
- **WHEN** project has `IsModLib=false` or property is not set
- **THEN** ILRepack merging SHALL proceed according to EnableILRepack setting

## REMOVED Requirements

### Requirement: Basic Mod Metadata Generation
The build system SHALL automatically generate basic mod metadata files for all mod projects.

**Reason**: IsModLib projects are shared libraries and should not have mod metadata as they are not deployed as standalone mods.
**Migration**: IsModLib projects will skip info.ini generation, while final release mods will continue to generate metadata normally.

#### Scenario: Skip metadata generation for library projects
- **WHEN** project has `IsModLib=true`
- **THEN** info.ini generation SHALL be skipped
- **AND** EnsureInfoIni target SHALL not execute
- **WHEN** project has `IsModLib=false` or property is not set
- **THEN** info.ini generation SHALL proceed normally