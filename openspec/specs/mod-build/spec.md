# mod-build Specification

## Purpose

Defines the build system capabilities for mod development including automatic generation and synchronization of mod metadata files, particularly info.ini files with proper name field handling.

## ADDED Requirements

### Requirement: INI File Name Synchronization
The build system SHALL automatically synchronize the name field in info.ini files with the ModName property from MSBuild configuration.

#### Scenario: INI Name Mismatch Detection and Correction
- **WHEN** ensure-info-ini.csx script runs and finds an existing info.ini file
- **AND** the name field in info.ini differs from the provided ModName parameter
- **THEN** the script SHALL update the name field to match ModName
- **AND** SHALL log the synchronization action

### Requirement: INI File Validation
The build system SHALL validate info.ini files for required fields and proper formatting during build processes.

#### Scenario: Missing Name Field Handling
- **WHEN** processing an info.ini file that lacks a name field
- **THEN** the system SHALL add the name field with the ModName value
- **AND** SHALL preserve all existing fields and formatting

#### Scenario: Invalid INI Format Recovery
- **WHEN** an info.ini file contains malformed name field entries
- **THEN** the system SHALL correct the format and set the value to ModName
- **AND** SHALL log the correction action for transparency

### Requirement: Build Target Integration
The MSBuild targets SHALL integrate INI name synchronization into the existing build pipeline.

#### Scenario: EnsureInfoIni Target Enhancement
- **WHEN** the EnsureInfoIni target executes
- **THEN** it SHALL invoke ensure-info-ini.csx with ModName parameter
- **AND** the script SHALL perform name synchronization on existing files
- **AND** SHALL continue execution even if synchronization fails

### Requirement: Backward Compatibility
The INI name synchronization functionality SHALL maintain backward compatibility with existing mod projects.

#### Scenario: Existing INI Files Preservation
- **WHEN** processing existing info.ini files with matching name fields
- **THEN** the system SHALL make no modifications to the file
- **AND** SHALL preserve original formatting and comments
- **AND** SHALL not touch modification timestamps when no changes are needed

#### Scenario: Manual Override Support
- **WHEN** developers need to maintain different name field values
- **THEN** the system SHALL provide a mechanism to disable automatic synchronization
- **AND** SHALL respect developer intent for special naming requirements
## Requirements
### Requirement: INI File Name Synchronization
The build system SHALL automatically synchronize the name field in info.ini files with the ModName property from MSBuild configuration using a centralized property resolution target.

#### Scenario: Centralized Property Resolution for INI Synchronization
- **WHEN** the `ResolveDuckySdkProperties` target executes before `EnsureInfoIni`
- **THEN** the system SHALL determine if INI synchronization should occur based on `IsModLib`, `DeployMod`, and `ModName` properties
- **AND** SHALL pass this decision to `EnsureInfoIni` without recalculating the same logic

#### Scenario: Early Validation for Configuration Conflicts
- **WHEN** the `ResolveDuckySdkProperties` target detects conflicting configurations (e.g., `IsModLib=true` with `DeployMod=true`)
- **THEN** the system SHALL emit clear warning messages explaining the conflict resolution
- **AND** SHALL proceed with the most restrictive setting

### Requirement: INI File Validation
The build system SHALL validate info.ini files for required fields and proper formatting during build processes.

#### Scenario: Missing Name Field Handling
- **WHEN** processing an info.ini file that lacks a name field
- **THEN** the system SHALL add the name field with the ModName value
- **AND** SHALL preserve all existing fields and formatting

#### Scenario: Invalid INI Format Recovery
- **WHEN** an info.ini file contains malformed name field entries
- **THEN** the system SHALL correct the format and set the value to ModName
- **AND** SHALL log the correction action for transparency

### Requirement: Build Target Integration
The MSBuild targets SHALL integrate comprehensive directory creation and package resolution into the existing build pipeline.

#### Scenario: Enhanced GenerateBuildContext Target
- **WHEN** the GenerateBuildContext target executes
- **THEN** it SHALL create the obj directory before script execution
- **AND** SHALL invoke ContextJsonBuild.csx with all required parameters
- **AND** SHALL ensure proper package source resolution through nuget.config
- **AND** SHALL continue execution only when directory exists and script succeeds

### Requirement: Backward Compatibility
The INI name synchronization functionality SHALL maintain backward compatibility with existing mod projects.

#### Scenario: Existing INI Files Preservation
- **WHEN** processing existing info.ini files with matching name fields
- **THEN** the system SHALL make no modifications to the file
- **AND** SHALL preserve original formatting and comments
- **AND** SHALL not touch modification timestamps when no changes are needed

#### Scenario: Manual Override Support
- **WHEN** developers need to maintain different name field values
- **THEN** the system SHALL provide a mechanism to disable automatic synchronization
- **AND** SHALL respect developer intent for special naming requirements

### Requirement: Global Using Directives
The build system SHALL automatically provide global using directives for essential SDK, game engine, and third-party namespaces when Ducky.Sdk is referenced, with centralized property management for conditional imports.

#### Scenario: Conditional Game Engine Import Optimization
- **WHEN** the `ResolveDuckySdkProperties` target checks for game dependency availability
- **THEN** it SHALL set `_HasGameDependencies` property based on actual DLL existence
- **AND** SHALL prevent multiple targets from performing the same file system checks
- **AND** SHALL cache the result for the duration of the build

#### Scenario: Centralized Global Using Configuration
- **WHEN** `EnableGlobalUsing` property is resolved in `ResolveDuckySdkProperties`
- **THEN** all subsequent targets SHALL use the resolved value without re-evaluation
- **AND** global using ItemGroups SHALL be conditionally included based on the centralized decision

### Requirement: Global Using Configuration
The build system SHALL provide configuration options to control global using directive behavior.

#### Scenario: Global Using Enable/Disable
- **WHEN** developers set EnableGlobalUsing property to false
- **THEN** the system SHALL not automatically add any global using directives
- **AND** developers MUST use explicit using statements in their code

#### Scenario: Selective Global Using
- **WHEN** developers specify custom global using configuration
- **THEN** the system SHALL respect the custom namespace selection
- **AND** SHALL only include the explicitly configured namespaces

### Requirement: Global Using Backward Compatibility
Global using functionality SHALL maintain full backward compatibility with existing mod projects.

#### Scenario: Existing Project Compatibility
- **WHEN** existing projects are compiled with the new SDK version
- **THEN** all existing using statements SHALL continue to work without modification
- **AND** duplicate global usings SHALL not cause compilation errors

#### Scenario: Explicit Control Preservation
- **WHEN** developers prefer explicit using statements
- **THEN** they SHALL be able to disable automatic global usings
- **AND** maintain full control over namespace imports

### Requirement: LocalizationAssetsDir-Based Asset Generation Optimization
The build system SHALL optimize multi-language asset generation by using LocalizationAssetsDir-based centralized generation and asset distribution with intelligent caching and change detection.

#### Scenario: Centralized Localization Property Resolution
- **WHEN** the `ResolveDuckySdkProperties` target processes localization configuration
- **THEN** it SHALL resolve `_EffectiveLocalizationAssetsDir`, `_PrimaryLocalizationDir`, and `_HasMultipleLocalizationDirs` properties once
- **AND** SHALL make these properties available to all localization targets without recalculation

#### Scenario: Smart Cache-Based Key Extraction
- **WHEN** the `ExtractLKeysJson` target executes with caching enabled
- **THEN** it SHALL compare generated file timestamps with cache metadata
- **AND** SHALL skip key extraction if no source files have changed since last successful extraction
- **AND** SHALL use cached JSON files for subsequent localization steps

#### Scenario: Change-Based Asset Copying
- **WHEN** the `CopyLocalizationAssets` target processes multiple directories
- **THEN** it SHALL compare source and target file timestamps before copying
- **AND** SHALL only copy files that have changed or are missing in target directories
- **AND** SHALL preserve file permissions and attributes during copying

### Requirement: Asset Copy Distribution System
The build system SHALL implement enhanced logging for asset distribution operations with detailed file tracking and error reporting.

#### Scenario: Detailed Asset Copy Logging
- **WHEN** localization assets are copied between directories
- **THEN** the system SHALL log source and target paths for each copy operation
- **AND** SHALL report file sizes and timestamps for copied files
- **AND** SHALL identify and skip unchanged files with reasoning
- **AND** SHALL provide clear error messages for permission or space issues

#### Scenario: Build Performance Monitoring
- **WHEN** building projects with multiple LocalizationAssetsDir directories
- **THEN** the system SHALL report total time spent in each build phase
- **AND** SHALL identify slowest CSX scripts and targets
- **AND** SHALL provide optimization suggestions when phases exceed expected duration

### Requirement: UpdateLocalesCsv Target Enhancement
The `UpdateLocalesCsv` target SHALL be enhanced to support centralized generation and distribution workflow.

#### Scenario: Centralized Generation Execution
- **WHEN** the `UpdateLocalesCsv` target executes with multiple LocalizationAssetsDir entries
- **THEN** it SHALL perform centralized generation in the project's local assets folder
- **AND** SHALL invoke asset copying to all LocalizationAssetsDir targets
- **AND** SHALL perform consistency validation before completion

#### Scenario: Build Performance Improvement
- **WHEN** building projects with multiple LocalizationAssetsDir directories
- **THEN** the `UpdateLocalesCsv` target SHALL complete faster than the previous implementation
- **AND** SHALL reduce overall build time by eliminating redundant generation

### Requirement: ExtractLKeysJson Target Enhancement
The `ExtractLKeysJson` target SHALL be enhanced to support centralized generation.

#### Scenario: JSON File Centralization
- **WHEN** `LocalizationAssetsDir` contains multiple directory paths
- **THEN** the `ExtractLKeysJson` target SHALL generate `lkeys.json` files only in the primary location
- **AND** SHALL copy generated JSON files to all LocalizationAssetsDir targets
- **AND** SHALL ensure consistent JSON content across all directories

### Requirement: Conditional Game Dependencies Import
The build system SHALL provide detailed logging for game dependency detection and reference addition processes.

#### Scenario: Game Dependency Detection Logging
- **WHEN** the ResolveBuildProperties target checks for game dependencies
- **THEN** the system SHALL log all searched paths and file existence checks
- **AND** SHALL report which game dependencies were found and which are missing
- **AND** SHALL display the resolved dependency properties in detailed mode

#### Scenario: Reference Addition Transparency
- **WHEN** the AddDuckyManagedReferences target executes
- **THEN** the system SHALL log all reference addition attempts with full paths
- **AND** SHALL clearly identify which references were successfully added
- **AND** SHALL provide warnings for missing dependencies with suggested solutions

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

### Requirement: Build Result Tracking System
The build system SHALL provide comprehensive tracking of individual build step execution status with persistent storage and visual reporting capabilities.

#### Scenario: Build Result Creation and Persistence
- **WHEN** any build script executes through entry.csx
- **THEN** the system SHALL create a BuildResult instance to track step execution
- **AND** SHALL persist the results to buildResult.json alongside buildContext.json
- **AND** SHALL include step name, status, execution time, and error information

#### Scenario: Step Status Tracking
- **WHEN** individual script libraries execute (ValidateProjectPathLib, ExtractLocalizationKeysLib, etc.)
- **THEN** the system SHALL record step status as Success, Failed, or Skipped
- **AND** SHALL capture start time, end time, and duration for each step
- **AND** SHALL store error messages and stack traces for failed steps

#### Scenario: Build Result Visualization
- **WHEN** printResult.csx script is executed
- **THEN** the system SHALL display build results using ASCII art headers and emoji indicators
- **AND** SHALL show step-by-step status with ✅ for success, ❌ for failure, ⏭️ for skipped
- **AND** SHALL display timing information and overall build summary statistics
- **AND** SHALL use colored output to highlight different status types

#### Scenario: Build Result Integration
- **WHEN** entry.csx executes any script library
- **THEN** it SHALL create or load BuildResult from buildResult.json
- **AND** SHALL update step results immediately after each library execution
- **AND** SHALL save the updated BuildResult after each step completion
- **AND** SHALL continue normal execution regardless of BuildResult tracking success/failure

#### Scenario: Build Result Display Integration
- **WHEN** ExecutePostBuildScripts target completes all script library executions
- **THEN** the system SHALL automatically execute printResult.csx to display build results
- **AND** SHALL use the same BuildContext JSON file and BuildResult for context
- **AND** SHALL display results after all build steps are complete
- **AND** SHALL run printResult.csx as the final step in the orchestration workflow

#### Scenario: Error Handling and Resilience
- **WHEN** BuildResult tracking encounters errors (file access, serialization failures)
- **THEN** the system SHALL log warnings but continue normal build execution
- **AND** SHALL not fail the build process due to result tracking issues
- **AND** SHALL provide fallback behavior when BuildResult persistence is unavailable

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

### Requirement: Dependency-Aware BuildContext Properties
BuildContext SHALL provide computed properties for dependency-aware build decisions.

#### Scenario: Dependency-aware property computation
- **WHEN** BuildContext properties are accessed
- **THEN** MainAssemblyPath SHALL be available after UpdateBuildContextAfterBuildLib execution
- **AND** DependencyAssemblies SHALL be populated by scanning OutputPath
- **AND** HasDependencies SHALL be computed from DependencyAssemblies count
- **AND** ShouldUseILRepack SHALL consider both EnableILRepack and HasDependencies

#### Scenario: Build context initialization
- **WHEN** BuildContext.LoadFromProjectDirectory is called
- **THEN** basic properties SHALL be initialized without dependency scanning
- **AND** dependency properties SHALL be populated later by UpdateBuildContextAfterBuildLib
- **AND** logging SHALL indicate when dependencies are not yet available

#### Scenario: Post-compilation context update
- **WHEN** UpdateBuildContextAfterBuildLib runs
- **THEN** it SHALL load the existing BuildContext from JSON
- **AND** perform dependency detection and update properties
- **AND** save the updated context back to the same JSON file
- **AND** logging SHALL report discovered dependencies

### Requirement: MessageHub Host Auto-Startup
The SDK SHALL automatically start a MessageHub host when the first mod with host functionality enabled initializes.

#### Scenario: First mod starts MessageHub host
- **WHEN** a mod loads and `EnableMessageHubHost` is true (default)
- **AND** no other MessageHub host is currently running
- **THEN** the mod becomes the active MessageHub host
- **AND** other mods can connect to it for inter-mod communication

#### Scenario: Subsequent mods detect existing host
- **WHEN** a mod loads and `EnableMessageHubHost` is true
- **AND** another MessageHub host is already running
- **THEN** the mod connects to the existing host as a client
- **AND** does not start a new host instance

### Requirement: MessageHub Host Configuration
ModBehaviourBase SHALL provide configuration to control MessageHub host behavior.

#### Scenario: Mod disables MessageHub host functionality
- **WHEN** a mod sets `EnableMessageHubHost` to false
- **THEN** the mod does not attempt to start or become a MessageHub host
- **AND** can still connect to other mods' MessageHub hosts

#### Scenario: Mod checks host status
- **WHEN** a mod needs to know if it's the MessageHub host
- **THEN** `IsMessageHubHost` property returns true if the mod is the active host
- **AND** returns false if it's a client or host functionality is disabled

### Requirement: MessageHub Backward Compatibility
Existing mods SHALL continue to work without any code changes.

#### Scenario: Existing mod without MessageHub configuration
- **WHEN** an existing mod loads without any MessageHub configuration
- **THEN** the mod works exactly as before
- **AND** automatically gets MessageHub host functionality (can be opted out)

#### Scenario: Existing MessageHubHost mod users
- **WHEN** users currently using Ducky.MessageHubHost mod
- **THEN** they can remove the separate MessageHubHost mod
- **AND** any other mod can become the host automatically

### Requirement: Host Lifecycle Management
The MessageHub host SHALL remain running permanently once started.

#### Scenario: Host remains running after mod disables
- **WHEN** the active MessageHub host mod is disabled
- **THEN** the host continues running indefinitely
- **AND** other mods can continue using it
- **AND** no shutdown or cleanup is performed

### Requirement: Message Queue Processing
The ModHttpV1 system SHALL process messages from the queue while validating their TTL before delivery.

#### Scenario: Processing messages with TTL check
- **WHEN** processing messages from the queue
- **THEN** the system SHALL check if message timestamp + TTL < current time
- **AND** expired messages SHALL be skipped and removed from queue
- **AND** valid messages SHALL be delivered to the registered callback
- **AND** processing SHALL continue with next message if current is expired

#### Scenario: Queue management with expired messages
- **WHEN** the queue contains expired messages
- **THEN** expired messages SHALL be removed during normal processing
- **AND** the queue size SHALL reflect only non-expired messages
- **AND** periodic cleanup SHALL run every 30 seconds to remove expired messages

#### Scenario: Expired messages cleanup
- **WHEN** CleanupExpiredMessagesAsync runs
- **THEN** all expired messages SHALL be removed regardless of handler registration
- **AND** valid messages SHALL be preserved in their original order
- **AND** cleanup statistics SHALL be logged when expired messages are found

### Requirement: Message TTL Support
The ModHttpV1 message system SHALL support Time-To-Live (TTL) for messages to prevent indefinite queuing and resource waste.

#### Scenario: Message expires before processing
- **WHEN** a message has been in queue longer than its TTL (default 60 seconds)
- **THEN** the message SHALL be discarded during processing
- **AND** a debug log entry SHALL be created indicating message expiration

#### Scenario: Message processed within TTL
- **WHEN** a message is processed before its TTL expires
- **THEN** the message SHALL be delivered normally to the registered handler
- **AND** no TTL-related logging SHALL occur

#### Scenario: Custom TTL configuration
- **WHEN** the system needs different TTL values
- **THEN** the TTL value SHALL be configurable via system settings
- **AND** the default TTL SHALL remain 60 seconds if not configured

### Requirement: ModHttpV1 Configurable Logging
ModHttpV1 and ModHttpV1Proxy components SHALL respect the logging configuration from ModOptions to control log output.

#### Scenario: ModHttpV1 Respects Logging Configuration
- **WHEN** ModHttpV1 performs operations that generate logs
- **AND** ModOptions.EnableHttpV1Logging is false
- **THEN** ModHttpV1 SHALL NOT output any logs
- **AND** SHALL skip log message formatting for performance

#### Scenario: ModHttpV1Proxy Respects Logging Configuration
- **WHEN** ModHttpV1Proxy performs operations that generate logs
- **AND** ModOptions.EnableHttpV1Logging is false
- **THEN** ModHttpV1Proxy SHALL NOT output any logs
- **AND** SHALL skip log message formatting for performance

#### Scenario: Logging Enabled Behavior
- **WHEN** ModOptions.EnableHttpV1Logging is true
- **THEN** both ModHttpV1 and ModHttpV1Proxy SHALL log normally
- **AND** all existing log messages SHALL be preserved

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

