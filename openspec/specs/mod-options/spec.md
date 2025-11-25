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

### Requirement: LanguageSupport Attribute Basic Functionality
The `LanguageSupportAttribute` SHALL specify which languages should have CSV files and translation support generated for localization keys.

#### Scenario: Language Support Specification
- **WHEN** `LanguageSupport("en", "zh", "fr")` is applied to the LK class
- **THEN** the system SHALL generate CSV files only for the specified languages
- **AND** SHALL include language codes in the generated JSON metadata

#### Scenario: Default Language Behavior
- **WHEN** `Language()` is called without parameters
- **THEN** it SHALL default to English ("en")
- **AND** SHALL generate a single CSV file for English

### Requirement: Source Generator Language Processing
The `DuckyLocalizationGenerator` SHALL handle "all" keyword expansion during LK class processing.

#### Scenario: All Keyword Detection in Generator
- **WHEN** the generator encounters "all" in the LanguageSupport attribute
- **THEN** it SHALL expand "all" to the complete language list
- **AND** SHALL include the expanded list in the generated JSON metadata

#### Scenario: JSON Metadata with All Languages
- **WHEN** "all" is specified in LanguageSupport attribute
- **THEN** the generated JSON metadata SHALL contain the complete expanded language array
- **AND** SHALL not contain the "all" keyword itself

### Requirement: LanguageSupport Attribute Constructor
The `LanguageSupportAttribute` constructor SHALL process the "all" keyword to expand it to the full language list while maintaining backward compatibility.

#### Scenario: Backward Compatibility Preservation
- **WHEN** existing code uses `LanguageSupport("en", "zh", "fr")` with explicit language codes
- **THEN** the system SHALL continue to work exactly as before
- **AND** SHALL generate CSV files only for the explicitly specified languages

#### Scenario: Empty Constructor Default
- **WHEN** `Language()` is called without parameters
- **THEN** it SHALL continue to default to English ("en") as before
- **AND** SHALL not expand to the full language list

### Requirement: LanguageSupport "all" Keyword Support
The `LanguageSupportAttribute` SHALL accept "all" as a special keyword to generate localization files for all supported languages.

#### Scenario: All Languages Generation
- **WHEN** `LanguageSupport("all")` is applied to the LK class
- **THEN** the system SHALL generate CSV files for all predefined supported languages
- **AND** each language SHALL receive its own language-specific CSV file and directory

#### Scenario: Mixed All and Explicit Languages
- **WHEN** `LanguageSupport("all", "custom-lang")` is applied with "all" plus additional language codes
- **THEN** the system SHALL treat "all" as the primary directive
- **AND** additional explicit languages SHALL be included in the generated files
- **AND** duplicate language codes SHALL be deduplicated

#### Scenario: Case-Insensitive All Detection
- **WHEN** `LanguageSupport("ALL")` or `LanguageSupport("All")` is used
- **THEN** the system SHALL recognize the keyword regardless of case
- **AND** SHALL generate files for all supported languages

### Requirement: Comprehensive Language List
The LanguageSupport attribute SHALL provide a predefined comprehensive list of commonly supported languages.

#### Scenario: Built-in Language Access
- **WHEN** `LanguageSupportAttribute.AllLanguages` property is accessed
- **THEN** it SHALL return an array of all supported language codes
- **AND** the list SHALL include major world languages and regional variants

#### Scenario: Language List Composition
- **WHEN** "all" keyword is expanded
- **THEN** the generated list SHALL include: de, en, es, fr, ja, ko, pt, ru, zh-hant, zh
- **AND** all language codes SHALL be lowercase and follow ISO 639-1 standards where applicable

