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

### Requirement: Global Using Directives
The build system SHALL automatically provide global using directives for essential SDK, game engine, and third-party namespaces when Ducky.Sdk is referenced.

#### Scenario: SDK Namespaces Global Import
- **WHEN** a project references Ducky.Sdk with global usings enabled
- **THEN** the system SHALL automatically import core SDK namespaces including Ducky.Sdk.ModBehaviours, Ducky.Sdk.Logging, Ducky.Sdk.Attributes, Ducky.Sdk.Options, Ducky.Sdk.Contracts, and Ducky.Sdk.Localizations
- **AND** developers SHALL be able to use SDK types without explicit using statements

#### Scenario: Game Engine Namespaces Global Import
- **WHEN** a project references Ducky.Sdk with global usings enabled
- **THEN** the system SHALL automatically import game engine namespaces including TeamSoda, FOW, SodaLocalization, and commonly used Unity namespaces
- **AND** developers SHALL have direct access to game engine types and APIs

#### Scenario: Third-party Library Global Import
- **WHEN** a project references Ducky.Sdk with global usings enabled
- **THEN** the system SHALL automatically import frequently used third-party namespaces such as DG.Tweening (DOTween), Newtonsoft.Json, UniTask, and Steamworks
- **AND** developers SHALL be able to use these libraries without manual using statements

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

