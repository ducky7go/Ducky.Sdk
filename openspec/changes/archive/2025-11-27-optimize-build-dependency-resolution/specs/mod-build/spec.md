## ADDED Requirements

### Requirement: Post-Compilation BuildContext Updates
The build system SHALL provide a mechanism to update BuildContext with post-compilation information.

#### Scenario: UpdateBuildContextAfterBuildLib execution
- **WHEN** CoreCompile target completes successfully
- **THEN** the system SHALL execute UpdateBuildContextAfterBuildLib target
- **AND** load the existing BuildContext from JSON
- **AND** perform post-compilation analysis and updates
- **AND** save the updated BuildContext back to JSON file

### Requirement: BuildContext Dependency Resolution
The build system SHALL compute mod dependency information after compilation is complete.

#### Scenario: Dependency detection after compilation
- **WHEN** UpdateBuildContextAfterBuildLib script runs
- **THEN** the system SHALL scan the output directory for compiled assemblies
- **AND** identify dependency assemblies that are not part of the game's managed directory
- **AND** store the dependency list in BuildContext.DependencyAssemblies property
- **AND** update the BuildContext JSON file with the new dependency information

#### Scenario: Main assembly identification
- **WHEN** UpdateBuildContextAfterBuildLib script runs
- **THEN** the system SHALL locate the primary mod assembly based on project name
- **AND** store the path in BuildContext.MainAssemblyPath property
- **AND** ensure the main assembly is excluded from the dependency list

### Requirement: Dependency-Aware Build Decisions
BuildContext SHALL provide computed properties to guide build process decisions based on dependency information.

#### Scenario: ILRepack decision optimization
- **WHEN** EnableILRepack is true but no dependencies exist
- **THEN** BuildContext.ShouldUseILRepack SHALL return false
- **AND** build system SHALL skip ILRepack processing
- **AND** use the main assembly directly for deployment

#### Scenario: Dependency presence detection
- **WHEN** build processes need to know if dependencies exist
- **THEN** BuildContext.HasDependencies SHALL return true if DependencyAssemblies contains items
- **AND** return false if the dependency list is empty
- **AND** ModDeploy SHALL use this to determine copying strategy

### Requirement: Consistent Dependency Information
BuildContext SHALL ensure dependency information is consistent across all build phases.

#### Scenario: Single dependency scan
- **WHEN** BuildContext is initialized
- **THEN** dependency scanning SHALL happen only once
- **AND** ILRepack and ModDeploy SHALL use the same dependency list
- **AND** no duplicate scanning SHALL occur during build process

#### Scenario: Dependency information serialization
- **WHEN** UpdateBuildContextAfterBuildLib updates BuildContext
- **THEN** dependency information SHALL be saved to the JSON context file
- **AND** subsequent build steps loading from JSON SHALL get complete dependency data
- **AND** no re-scanning SHALL occur when context is loaded from cache

#### Scenario: BuildContext update persistence
- **WHEN** UpdateBuildContextAfterBuildLib completes dependency detection
- **THEN** the updated BuildContext SHALL be saved to the original JSON file
- **AND** preserve all existing BuildContext properties
- **AND** ensure atomic file write to prevent corruption

## ADDED Requirements

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