## MODIFIED Requirements

### Requirement: DeployMod=false Execution Control
The SDK SHALL prevent any mod deployment operations when `DeployMod` is set to `false`, ensuring consistent and predictable behavior.

#### Scenario: DeployMod=false prevents all deployment
- **WHEN** a build completes with `DeployMod=false`
- **THEN** the system SHALL NOT execute any copy or packaging operations
- **AND** SHALL ensure deletion happens after any potential operations
- **AND** SHALL log the deployment prevention clearly

#### Scenario: DeployMod=false deletion success
- **WHEN** a build completes with `DeployMod=false` and `DuckovFolder` is configured
- **AND** `ModName` is specified and not a library project (`IsModLib!=true`)
- **AND** the mod directory exists at the target location
- **THEN** the system SHALL delete the entire mod directory and all its contents
- **AND** log the successful deletion with the directory path
- **AND** verify the deletion was completed successfully

#### Scenario: DeployMod=false directory doesn't exist
- **WHEN** a build completes with `DeployMod=false`
- **AND** the target mod directory does not exist
- **THEN** the system SHALL log an informational message that no cleanup was needed
- **AND** continue the build process without errors

#### Scenario: DeployMod=false missing configuration
- **WHEN** a build completes with `DeployMod=false`
- **AND** required properties (`DuckovFolder`, `ModName`) are missing
- **THEN** the system SHALL skip deletion with a warning about missing configuration
- **AND** continue the build process without errors

#### Scenario: DeployMod=false library project
- **WHEN** a build completes with `DeployMod=false`
- **AND** the project is marked as a library (`IsModLib=true`)
- **THEN** the system SHALL skip deletion as library projects are not deployed
- **AND** log an informational message explaining the skip

#### Scenario: DeployMod=false prevents ILRepack operations
- **WHEN** `DeployMod=false` and `EnableILRepack=true`
- **THEN** the system SHALL skip `PackModWithILRepack` target execution
- **AND** log that ILRepack operations are disabled due to DeployMod=false
- **AND** not create any mod files or directories

#### Scenario: DeployMod=false prevents dependency copying
- **WHEN** `DeployMod=false` and `EnableILRepack!=true`
- **THEN** the system SHALL skip `CopyMissingDependencies` target execution
- **AND** log that dependency copying is disabled due to DeployMod=false
- **AND** not create Dependency folders or copy DLL files

#### Scenario: DeployMod=false execution order guarantee
- **WHEN** `DeployMod=false` and the mod directory exists
- **THEN** the system SHALL ensure deletion runs after any potential deployment targets
- **AND** use proper target dependencies to guarantee execution order
- **AND** prevent race conditions between deletion and copy operations