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

## ADDED Requirements

### Requirement: SDK-Level CI Path Defaults Based on Git Root
The SDK SHALL automatically apply default path values for `ManagedDirectory` and `ModsDirectory` when building in CI environments, using Git repository root as the base path for consistency across project structures.

#### Scenario: Automatic CI Defaults Applied Using Git Root
- **WHEN** a project referencing Ducky.Sdk builds in a CI environment (`CI=true`)
- **AND** no explicit `ManagedDirectory` or `ModsDirectory` environment variables are set
- **AND** no `DuckovFolder` is configured
- **THEN** the SDK SHALL detect the Git repository root by finding the `.git` directory
- **AND** the SDK SHALL automatically set `ManagedDirectory` to `$(GitRoot)/Managed`
- **AND** the SDK SHALL automatically set `ModsDirectory` to `$(GitRoot)/artifacts/Mods`
- **AND** the build SHALL complete successfully without any manual configuration

#### Scenario: Git Root Detection at Multiple Depths
- **WHEN** a project is located at various depths within the repository (e.g., `src/MyMod`, `samples/MyMod`, or repository root)
- **AND** the SDK searches for the `.git` directory by traversing upward
- **THEN** the SDK SHALL find the Git repository root regardless of project depth
- **AND** all projects SHALL use the same Git root-based paths for `ManagedDirectory` and `ModsDirectory`

#### Scenario: Explicit Override Takes Precedence
- **WHEN** a user explicitly sets `ManagedDirectory` or `ModsDirectory` via environment variable
- **THEN** the explicit value SHALL take precedence over SDK defaults
- **AND** the SDK SHALL NOT override the user-provided value

#### Scenario: Local Development Unaffected
- **WHEN** building locally (not in CI environment)
- **AND** `DuckovFolder` is configured
- **THEN** the SDK SHALL derive paths from `DuckovFolder` as before
- **AND** CI defaults SHALL NOT be applied

#### Scenario: DuckovFolder Takes Precedence Over CI Defaults
- **WHEN** building in CI environment
- **AND** `DuckovFolder` is explicitly set
- **THEN** paths SHALL be derived from `DuckovFolder` instead of CI defaults
- **AND** CI defaults SHALL NOT be applied when `DuckovFolder` is available

#### Scenario: Zero-Configuration CI Build
- **WHEN** a developer creates a new project referencing Ducky.Sdk
- **AND** pushes the project to a CI/CD system (GitHub Actions, Azure Pipelines, etc.)
- **AND** the CI system sets the `CI=true` environment variable
- **THEN** the project SHALL build successfully without any additional CI configuration
- **AND** mod artifacts SHALL be output to `$(GitRoot)/artifacts/Mods`

#### Scenario: Fallback When Git Not Detected
- **WHEN** the SDK cannot find a `.git` directory
- **AND** building in CI environment without explicit paths
- **THEN** the SDK SHALL fallback to using the project directory as the base
- **AND** the build SHALL attempt to complete with project-relative paths
