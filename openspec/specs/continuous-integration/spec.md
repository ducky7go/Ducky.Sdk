# continuous-integration Specification

## Purpose
Defines the continuous integration and deployment capabilities for the Ducky.Sdk project, including automated building, testing, and publishing to multiple package repositories (NuGet.org and MyGet).
## Requirements
### Requirement: MyGet Package Publishing
The continuous integration system SHALL publish generated NuGet packages to both NuGet.org and MyGet repositories for tag and main branch builds.

#### Scenario: Successful Dual Publishing
- **WHEN** a build is triggered on main branch or semver tag
- **AND** the build produces .nupkg files
- **THEN** the packages SHALL be published to both NuGet.org and MyGet repositories
- **AND** the publishing steps SHALL continue even if one repository fails

#### Scenario: MyGet Publishing Failure Continues
- **WHEN** MyGet publishing fails due to network issues or authentication problems
- **THEN** the workflow SHALL continue and attempt NuGet.org publishing
- **AND** the overall build status SHALL be determined by the successful NuGet.org publication

#### Scenario: NuGet Publishing Failure Continues
- **WHEN** NuGet.org publishing fails after successful MyGet publishing
- **THEN** the workflow SHALL mark the step as failed but continue execution
- **AND** any subsequent workflow steps SHALL still be processed

### Requirement: MyGet Authentication
The publishing workflow SHALL authenticate with MyGet using the MYGET_API_KEY repository secret.

#### Scenario: MyGet API Key Usage
- **WHEN** publishing packages to MyGet
- **THEN** the workflow SHALL use the MYGET_API_KEY secret for authentication
- **AND** the API key SHALL be securely passed to the dotnet nuget push command
- **AND** the key SHALL not be exposed in workflow logs

### Requirement: CI Build Path Configuration
The CI system SHALL support configurable build paths via environment variables for automated mod building without Steam installation.

#### Scenario: Environment Variable Property Injection
- **WHEN** CI workflow sets `ManagedDirectory` and `ModsDirectory` environment variables
- **THEN** MSBuild SHALL receive these as properties during build
- **AND** the build SHALL complete using the specified paths
- **AND** SHALL NOT require Steam installation
- **AND** SHALL skip SteamFolder validation when `CI=true`

#### Scenario: GitHub Actions CI Build
- **WHEN** GitHub Actions workflow runs with environment variables set
- **THEN** `dotnet build` SHALL use the injected properties for path resolution
- **AND** mod artifacts SHALL be produced in the configured output directory
- **AND** the build SHALL succeed without accessing Steam directory

#### Scenario: Cross-Platform CI Compatibility
- **WHEN** CI builds run on different platforms (Windows, Linux, macOS)
- **THEN** path overrides SHALL work correctly on all platforms
- **AND** path separators SHALL be handled appropriately
- **AND** builds SHALL produce consistent artifacts across platforms

#### Scenario: CI Environment Detection
- **WHEN** the build system detects `CI` environment variable is set
- **THEN** SteamFolder validation SHALL be skipped
- **AND** builds with explicit directory paths SHALL succeed
- **AND** builds without explicit paths SHALL still fail with clear error message

