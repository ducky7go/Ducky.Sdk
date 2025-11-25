## ADDED Requirements

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

## MODIFIED Requirements

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