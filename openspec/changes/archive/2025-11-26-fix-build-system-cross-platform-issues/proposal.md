# Change: Fix Build System Cross-Platform Compatibility Issues

## Why
The current build system fails on Linux environments due to platform-specific dependencies and font availability issues. This prevents developers from using the SDK on non-Windows platforms and breaks the CI/CD pipeline.

## What Changes
- **GitHub-Style Preview Generation**: Replace font-based preview generation with geometric patterns similar to GitHub identicons
- **Signal Handling**: Fix ContextJsonBuild.csx segmentation fault by improving argument parsing and error handling
- **Localization Processing**: Fix UpdateLocalizationCsvLib to handle edge cases in directory resolution
- **Error Recovery**: Improve error handling and logging across all build scripts

## Impact
- Affected specs: mod-build
- Affected code: GeneratePreviewLib.cs, ContextJsonBuild.csx, UpdateLocalizationCsvLib.cs
- **BREAKING**: None - these are bug fixes only
- Platform support: Linux, macOS, Windows
- Developer experience: Improved cross-platform compatibility