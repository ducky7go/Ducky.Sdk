# mod-build Specification

## Purpose

Defines the build system capabilities for mod development including automatic generation and synchronization of mod metadata files, global using directive management, and enhanced developer experience features.

## ADDED Requirements

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