# Change: Handle IsModLib flag to skip non-essential automation tasks

## Why
Currently, projects marked with `IsModLib=true` (shared library projects) still execute all automation tasks intended for final release mod projects, including image generation, publishing, and ILRepack operations. This wastes build time and can cause unnecessary processing for projects that should only serve as code libraries.

## What Changes
- **MODIFIED**: Build process will check `IsModLib` property and skip non-essential automation tasks when true
- **ADDED**: New logic to conditionally disable the following tasks for IsModLib projects:
  - Preview PNG generation (`GeneratePreview` target)
  - Mod deployment and copying (`CopyToDuckov` target)
  - ILRepack assembly merging (`PackModWithILRepack` target)
  - Dependency copying (`CopyMissingDependencies` target)
- **MODIFIED**: Localization processing will remain enabled for IsModLib projects since shared libraries may contain translatable strings
- **REMOVED**: Basic info.ini generation will be skipped for IsModLib projects as they are not deployed mods
- **ADDED**: Split Ducky.Sdk.targets into multiple focused files for better maintainability:
  - `Ducky.Sdk.Validation.targets` - Path validation and folder checks
  - `Ducky.Sdk.Localization.targets` - Localization processing tasks
  - `Ducky.Sdk.Assets.targets` - Asset generation and copying tasks
  - `Ducky.Sdk.Packaging.targets` - ILRepack and deployment tasks
- **MODIFIED**: Ducky.Sdk.props remains for property initialization but moves IsModLib-specific logic to dedicated section

## Impact
- Affected specs: mod-build
- Affected code: Ducky.Sdk.targets, Ducky.Sdk.props, plus new modular target files
- Build performance improvement for library projects
- Cleaner separation between final release mods and shared libraries
- Improved code organization and maintainability through file modularization
- Easier testing and debugging of specific build phases