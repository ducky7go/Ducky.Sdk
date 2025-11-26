# mod-build Specification

## Purpose

Defines the build system capabilities for mod development including automatic generation and synchronization of mod metadata files, particularly info.ini files with proper name field handling, reliable package source resolution, and immediate version synchronization between SDK packaging and sample project updates.

## ADDED Requirements

### Requirement: Package Source Resolution
The build system SHALL ensure consistent package source resolution for Ducky.Sdk between IDE builds and script-based builds.

#### Scenario: Local Package Source Priority
- **WHEN** building sample projects or any project referencing Ducky.Sdk
- **THEN** the system SHALL prioritize the local duckylocal package source for Ducky.Sdk
- **AND** SHALL fall back to nuget.org for all other package dependencies
- **AND** SHALL prevent version conflicts between local and remote package sources

#### Scenario: Package Source Mapping Validation
- **WHEN** NuGet package restore is executed
- **THEN** the system SHALL respect explicit package source mappings
- **AND** SHALL resolve Ducky.Sdk to the latest local development version
- **AND** SHALL maintain stable package resolution across build environments

### Requirement: Build Directory Race Condition Prevention
The MSBuild targets SHALL prevent race conditions between build directory creation and CSX script execution.

#### Scenario: Obj Directory Pre-creation
- **WHEN** GenerateBuildContext target executes before CSX script invocation
- **THEN** the system SHALL create the obj directory if it doesn't exist
- **AND** SHALL ensure the directory creation completes before script execution
- **AND** SHALL proceed with BuildContext JSON generation only after directory exists

#### Scenario: Concurrent Build Safety
- **WHEN** multiple projects are built simultaneously
- **THEN** each project SHALL have its obj directory created independently
- **AND** SHALL prevent cross-project interference during directory creation
- **AND** SHALL maintain atomic directory creation operations

### Requirement: Build System Reliability
The build system SHALL provide consistent behavior between IDE builds and rebuild_samples.sh script execution.

#### Scenario: Version Increment Build Success
- **WHEN** rebuild_samples.sh script increments version and rebuilds samples
- **THEN** the build SHALL complete without Exit Code 135 errors
- **AND** SHALL successfully execute all CSX scripts
- **AND** SHALL maintain consistent behavior across subsequent builds

#### Scenario: First-time Build Consistency
- **WHEN** building in a clean environment for the first time
- **THEN** the system SHALL create all necessary directories before script execution
- **AND** SHALL resolve packages to correct local versions
- **AND** SHALL complete successfully without manual intervention

### Requirement: Direct Version Management
Sample projects SHALL use explicit Ducky.Sdk versions in csproj files instead of dynamic property references.

#### Scenario: Explicit Version Specification
- **WHEN** Sample project csproj files reference Ducky.Sdk
- **THEN** they SHALL use specific version numbers instead of $(LocalNuGetVersion) properties
- **AND** SHALL maintain version consistency across all sample projects
- **AND** SHALL allow immediate package resolution without property resolution delays

#### Scenario: Version Reference Stability
- **WHEN** building Sample projects independently
- **THEN** the system SHALL use hardcoded version references for stability
- **AND** SHALL not depend on nuget.props property inheritance
- **AND** SHALL ensure consistent package resolution across build environments

### Requirement: Dynamic Version Synchronization
The rebuild_samples.sh script SHALL automatically update Sample project version references using NuGet package management commands.

#### Scenario: Automatic Version Updates
- **WHEN** rebuild_samples.sh script creates a new SDK package version
- **THEN** it SHALL update all Sample project csproj files to reference the new version
- **AND** SHALL use `dotnet add package` commands for reliable version updates
- **AND** SHALL ensure immediate availability of the new version for subsequent builds

#### Scenario: Version Update Validation
- **WHEN** version updates are applied to Sample projects
- **THEN** the system SHALL verify successful package resolution
- **AND** SHALL confirm that all projects reference the same version
- **AND** SHALL fail the rebuild process if version updates fail

## MODIFIED Requirements

### Requirement: Build Target Integration
The MSBuild targets SHALL integrate comprehensive directory creation and package resolution into the existing build pipeline.

#### Scenario: Enhanced GenerateBuildContext Target
- **WHEN** the GenerateBuildContext target executes
- **THEN** it SHALL create the obj directory before script execution
- **AND** SHALL invoke ContextJsonBuild.csx with all required parameters
- **AND** SHALL ensure proper package source resolution through nuget.config
- **AND** SHALL continue execution only when directory exists and script succeeds