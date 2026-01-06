# continuous-integration Spec Delta

## ADDED Requirements

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
