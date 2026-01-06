# mod-build Spec Delta

## ADDED Requirements

### Requirement: CI-Friendly Directory Configuration
The build system SHALL support CI/CD environments by allowing existing MSBuild properties to override default directory paths without requiring Steam installation or `$(DuckovFolder)`.

#### Scenario: Explicit Managed Directory Override
- **WHEN** `ManagedDirectory` property is set explicitly (via environment variable or project configuration)
- **THEN** the build system SHALL use the specified directory for game assembly references
- **AND** SHALL NOT override the path with `$(DuckovFolder)`-derived value

#### Scenario: Explicit Mods Directory Override
- **WHEN** `ModsDirectory` property is set explicitly
- **THEN** the build system SHALL deploy mod artifacts to the specified directory
- **AND** SHALL NOT override the path with `$(DuckovFolder)/Mods/`-derived value

#### Scenario: Property Priority Resolution
- **WHEN** both explicit override and `$(DuckovFolder)` are available
- **THEN** explicitly set properties SHALL take precedence over derived paths
- **AND** derived paths SHALL be used as fallback when overrides are not set

#### Scenario: CI Build Without Steam Directory
- **WHEN** building in CI environment with `ManagedDirectory` and `ModsDirectory` set
- **AND** `$(DuckovFolder)` is not set
- **AND** `CI` environment variable is set to `true`
- **THEN** the build SHALL complete successfully without requiring Steam directory validation
- **AND** mod artifacts SHALL be deployed to the specified output directory

#### Scenario: Local Development Backward Compatibility
- **WHEN** building locally with `$(DuckovFolder)` set
- **AND** neither override property is specified
- **THEN** the build system SHALL derive paths from `$(DuckovFolder)` as before
- **AND** existing workflows SHALL continue to work without modification
