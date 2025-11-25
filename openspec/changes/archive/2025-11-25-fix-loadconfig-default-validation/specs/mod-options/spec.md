## ADDED Requirements

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

#### Scenario: Default Value Collection Type Validation
- **WHEN** `LoadConfig<T>()` is called with a missing key and a collection/array type `defaultValue`
- **THEN** the system SHALL serialize it to JSON as a complex type
- **AND** SHALL follow complex type validation logic