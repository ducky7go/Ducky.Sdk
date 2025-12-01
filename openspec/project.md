# Project Context

## Purpose
Ducky.Sdk is a comprehensive .NET SDK for developing mods for "Escape from Duckov" game. It provides:
- A NuGet package containing source generators, MSBuild tasks, and shared libraries
- Automatic localization key generation from constants
- Build-time deployment to game directories
- Automated mod metadata generation (info.ini, preview.png)
- Assembly merging via ILRepack for single-DLL distribution
- Optional Harmony runtime patching support (0Harmony)
- Strongly-typed mod development patterns

## Tech Stack
- .NET SDK 9.0 (Target Framework: .NET Standard 2.1)
- C# (primary development language)
- Roslyn incremental source generators (IIncrementalGenerator)
- MSBuild (.props and .targets files)
- ILRepack (for assembly merging)
- dotnet-script (for automation scripts)
- SixLabors.ImageSharp (for cross-platform image generation)
- GitHub Actions (CI/CD pipeline)
- NuGet package distribution

## Project Conventions

### Code Style
- Use C# coding conventions with Microsoft StyleCop rules
- Follow `dotnet format` for code formatting (validated in CI)
- XML documentation comments for public APIs
- Central package management via Directory.Packages.props
- GitVersion for semantic versioning in CI

### Architecture Patterns
- Source code distribution via contentFiles (not compiled assemblies)
- Three-component SDK architecture:
  1. Ducky.Sdk.Analyser (Roslyn source generator)
  2. Ducky.Sdk.Lib (shared library source code)
  3. Ducky.Sdk (MSBuild integration)
- Incremental source generation for performance
- MSBuild targets for automated build pipeline
- Single-DLL distribution via ILRepack

### Testing Strategy
- Unit tests in Sdk/Tests/ directory (dotnet test)
- Integration tests via Samples/ projects (temporary verification)
- End-to-end validation using rebuild_samples.sh script
- Comprehensive cache clearing for SDK changes testing
- Build automation for sample validation

### Git Workflow
- Main branch for development (triggers dev builds to NuGet)
- Tags matching *.*.* pattern for formal releases
- Pull requests must pass CI validation
- Semantic commit messages (conventional commits preferred)
- Automated release notes generation

## Domain Context

### Mod Development Concepts
- ModBehaviourBase: Base class for all mods with ModEnabled/ModDisabled lifecycle
- LK → L Pattern: Define localization keys in static class with constants
- Harmony Patching: Runtime IL manipulation via Harmony library
- Steam Workshop Integration: publishedFileId management
- Game Directory Structure: Managed/ contains game assemblies

### Game Integration
- Game assemblies located in $(DuckovFolder)/Managed/
- Mods deployed to $(DuckovFolder)/Mods/ModName/
- Unity-based game environment with TeamSoda engine
- Localization files stored in assets/Locales/

## Important Constraints
- SDK must be backward compatible with existing mods
- Source generators must target .NET Standard 2.0 (Roslyn requirement)
- No game source code modifications allowed
- All automation scripts must be cross-platform compatible
- SDK sources are distributed as source code (not compiled)
- Build targets execute in specific order (validate→compile→deploy)
- Localization CSV files must contain all keys from LKeys.All

## External Dependencies
- "Escape from Duckov" game (Unity/TeamSoda)
- Steam Workshop (for mod distribution)
- NuGet.org (for SDK package publishing)
- GitHub (for source code and CI/CD)
- 0Harmony (runtime patching library, optional)
- ILRepack (assembly merging tool)
- LibLog (logging abstraction)
