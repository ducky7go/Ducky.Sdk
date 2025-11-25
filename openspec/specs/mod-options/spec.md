# ModOptions Configuration Management Specification

## Purpose

Provides a configuration management system for mods with support for various data types, including primitive types, complex objects, collections, and specialized DateTime handling with robust validation and error recovery.

## Requirements

### Requirement: Simple Type Classification
The `IsSimpleType()` method SHALL correctly classify data types to determine appropriate storage mechanisms.

#### Scenario: Simple Type Detection
- **WHEN** `IsSimpleType()` is called with primitive types, enums, string, decimal, DateTime, etc.
- **THEN** it SHALL return `true` to indicate direct storage

### Requirement: DateTime Type Handling
ModOptions SHALL handle DateTime and DateTimeOffset types using Unix timestamp conversion.

#### Scenario: DateTime Serialization
- **WHEN** saving DateTime or DateTimeOffset types
- **THEN** the values SHALL be converted to Unix timestamps
- **AND** SHALL be stored as long integers

#### Scenario: DateTime Deserialization
- **WHEN** loading DateTime or DateTimeOffset types
- **THEN** the Unix timestamps SHALL be converted back to their original types

### Requirement: Collection Type JSON Serialization
ModOptions SHALL serialize collection types to JSON format for proper storage and retrieval.

#### Scenario: List<string> Save and Load
- **WHEN** a developer calls `SaveConfig` with a `List<string>` value
- **AND** the `List<string>` contains valid string data
- **THEN** the data SHALL be serialized to JSON format before storage
- **AND** `LoadConfig<List<string>>` SHALL deserialize the JSON back to the original list

### Requirement: LoadConfig Default Value Validation
The `LoadConfig<T>()` method SHALL validate the provided `defaultValue` before persisting it when a key doesn't exist, ensuring the same type validation and serialization logic as direct `SaveConfig<T>()` calls.

#### Scenario: Default Value Simple Type Validation
- **WHEN** `LoadConfig<T>()` is called with a missing key and a simple type `defaultValue`
- **THEN** the system SHALL apply the same type validation as `SaveConfig<T>()`
- **AND** SHALL persist the value using the appropriate simple type storage mechanism

#### Scenario: Default Value DateTime Type Validation
- **WHEN** `LoadConfig<T>()` is called with a missing key and a DateTime/DateTimeOffset `defaultValue`
- **THEN** the system SHALL convert the `defaultValue` to Unix timestamp
- **AND** SHALL persist it using the same time type logic as `SaveConfig<T>()`

#### Scenario: Default Value Complex Type Validation
- **WHEN** `LoadConfig<T>()` is called with a missing key and a complex type `defaultValue`
- **THEN** the system SHALL attempt JSON serialization of the `defaultValue`
- **AND** SHALL only persist if serialization succeeds
- **AND** SHALL return the original `defaultValue` even if serialization fails

#### Scenario: Default Value Serialization Failure Handling
- **WHEN** `LoadConfig<T>()` fails to serialize a complex type `defaultValue`
- **THEN** the system SHALL log the serialization failure
- **AND** SHALL return the original `defaultValue` without persisting
- **AND** SHALL not create an invalid configuration entry