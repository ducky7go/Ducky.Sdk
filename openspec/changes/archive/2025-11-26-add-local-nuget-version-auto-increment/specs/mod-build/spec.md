## ADDED Requirements

### Requirement: Local NuGet Package Version Auto-Increment
The build system SHALL provide automatic version increment functionality for local NuGet package development to avoid caching issues and manual version management.

#### Scenario: Auto-Increment Version Generation
- **WHEN** packToLocal.sh script runs without explicit version parameter
- **THEN** the system SHALL read current version from nuget.props file in project root
- **AND** SHALL increment the six-digit suffix by 1 (e.g., 0.0.000001-dev → 0.0.000002-dev)
- **AND** SHALL update the nuget.props file with the new version
- **AND** SHALL use the new version for packaging

#### Scenario: Version Props File Creation
- **WHEN** the nuget.props file does not exist in project root
- **THEN** the system SHALL create the file with initial version 0.0.000001-dev
- **AND** SHALL define LocalNuGetVersion property for project reference

#### Scenario: Manual Version Override
- **WHEN** packToLocal.sh script is called with explicit --version parameter
- **THEN** the system SHALL use the provided version without auto-increment
- **AND** SHALL NOT modify the nuget.props file
- **AND** SHALL maintain backward compatibility with existing workflow

#### Scenario: Sample Projects Version Reference
- **WHEN** Sample projects reference Ducky.Sdk package
- **THEN** they SHALL import the root nuget.props file
- **AND** SHALL use LocalNuGetVersion property for package version
- **AND** SHALL ensure consistent version across all sample projects

#### Scenario: Version Format Validation
- **WHEN** reading or updating version in nuget.props
- **THEN** the system SHALL validate format follows pattern 0.0.XXXXXX-dev
- **AND** SHALL ensure six-digit increment part stays within valid range (000001-999999)
- **AND** SHALL provide clear error messages for invalid formats