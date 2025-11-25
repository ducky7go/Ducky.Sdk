# Change: Add "all" support to LanguageSupport attribute

## Why
Enable developers to easily generate localization files for all supported languages without needing to manually specify each language code, improving developer experience and reducing maintenance overhead.

## What Changes
- Add support for "all" value in LanguageSupport attribute constructor
- Define comprehensive list of built-in supported languages
- Update source generator to handle "all" by expanding to full language list
- Update CSV generation script to process "all" languages
- Maintain backward compatibility with existing explicit language specifications

## Impact
- Affected specs: mod-options (LanguageSupport attribute functionality)
- Affected code:
  - `Sdk/SDKlibs/Ducky.Sdk.Lib/Attributes/LanguageSupportAttribute.cs`
  - `Sdk/Ducky.Sdk.Analyser/DuckyLocalizationGenerator.cs`
  - `Sdk/SDKlibs/Ducky.Sdk/scripts/update-locales-csv.csx`