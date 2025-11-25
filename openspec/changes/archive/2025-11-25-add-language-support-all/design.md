## Context
The LanguageSupport attribute currently requires developers to manually specify language codes. Developers want an easier way to generate files for all supported languages without maintaining a list of language codes manually.

## Goals / Non-Goals
- Goals:
  - Allow `LanguageSupport("all")` to generate files for all supported languages
  - Maintain full backward compatibility with existing usage
  - Define a comprehensive, well-maintained list of common languages
  - Keep the implementation simple and maintainable

- Non-Goals:
  - Dynamic language discovery from external sources
  - Custom language registration beyond the built-in list
  - Language filtering beyond the "all" vs explicit specification

## Decisions

- Decision: Implement "all" as a special keyword that expands to predefined language list
  - Rationale: Simple, predictable, and maintains existing behavior for explicit specifications
  - Alternatives considered:
    - Wildcard patterns (too complex for current use case)
    - External configuration files (adds maintenance overhead)
    - Enum-based approach (breaking change)

- Decision: Define built-in language list in the attribute class
  - Rationale: Centralized, easy to maintain, visible to developers
  - Alternatives considered:
    - Separate configuration file (adds file dependency)
    - Hardcoded in generator (less discoverable)

## Language List
The "all" keyword will expand to these language codes:
- Supported languages: de, en, es, fr, ja, ko, pt, ru, zh-hant, zh

## Risks / Trade-offs
- Risk: Language list may become outdated or miss important languages
  - Mitigation: Regular reviews and easy addition process

- Trade-off: Static list vs dynamic discovery
  - Chose static list for predictability and simplicity

## Migration Plan
1. Add "all" detection logic to LanguageSupportAttribute constructor
2. Add AllLanguages static property exposing the full language list
3. Update source generator to expand "all" to actual language codes
4. Update CSV script to handle the expanded language list
5. Add comprehensive tests for both "all" and explicit language specifications

## Open Questions
- Should we provide filtering options (e.g., "all-european", "all-asian")?
- How should we handle conflicts between "all" and explicit languages in the same attribute?