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

