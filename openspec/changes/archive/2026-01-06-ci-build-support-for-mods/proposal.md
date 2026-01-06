# CI Build Support for Mods

## Overview

Introduce CI/CD-friendly MSBuild properties to enable mod building and deployment without Steam directory dependencies. This change allows developers to build mods in continuous integration environments using environment variables to specify key paths.

## Background

Ducky.Sdk currently relies on `$(DuckovFolder)` for locating game assemblies and deployment targets. This property is derived from `$(SteamFolder)` using the pattern `$(SteamFolder)steamapps/common/Escape from Duckov/`. While this works well for local development with Steam installed, CI environments (GitHub Actions, Azure Pipelines, etc.) lack Steam installations, preventing automated mod builds.

## Problem Statement

1. **Steam Directory Hard Dependency**: Mod builds fail in CI without Steam installation
2. **Managed Directory Coupling**: Game assemblies path is tightly bound to `$(DuckovFolder)/Managed/`
3. **Mods Directory Coupling**: Deployment target is bound to `$(DuckovFolder)/Mods/`
4. **No CI Environment Variable Support**: Cannot override paths via CI environment variables

## Proposed Solution

### MSBuild Property Overrides

#### ManagedDirectory
- **Purpose**: Override game managed assemblies directory
- **Current Default**: `$(DuckovFolder)Duckov_Data/Managed/`
- **Priority**: Explicit value > DuckovFolder-derived value > default

#### ModsDirectory
- **Purpose**: Override mod deployment output directory
- **Current Default**: `$(DuckovFolder)Duckov_Data/Mods/`
- **Priority**: Explicit value > DuckovFolder-derived value > default

### Property Resolution Logic

The existing properties `ManagedDirectory` and `ModsDirectory` already exist in the SDK. This change makes them fully overridable via environment variables in CI environments by:

1. Allowing explicit values to override derived values
2. Making validation conditional on CI environment detection
3. Removing strict `DuckovFolder` requirement when paths are explicitly set

```xml
<!-- Existing pattern in Directory.Build.props -->
<PropertyGroup>
  <DuckovFolder Condition=" '$(DuckovFolder)' == '' AND '$(SteamFolder)' != '' ">
    $(SteamFolder)steamapps/common/Escape from Duckov/
  </DuckovFolder>
  <ModsDirectory>$(DuckovFolder)Duckov_Data/Mods/</ModsDirectory>
  <ManagedDirectory>$(DuckovFolder)Duckov_Data/Managed/</ManagedDirectory>
</PropertyGroup>

<!-- Updated: Allow explicit overrides -->
<PropertyGroup>
  <!-- When explicitly set, don't override with DuckovFolder-derived value -->
  <ManagedDirectory Condition=" '$(ManagedDirectory)' != '' ">
    $(ManagedDirectory)
  </ManagedDirectory>
  <ModsDirectory Condition=" '$(ModsDirectory)' != '' ">
    $(ModsDirectory)
  </ModsDirectory>
</PropertyGroup>

<!-- Updated: CI-aware validation -->
<Target Name="FailIfSteamFolderMissing" BeforeTargets="PrepareForBuild;Restore" Condition="'$(CI)' != 'true'">
  <Error Text="SteamFolder property must be set. Provide it via Local.props or -p:SteamFolder=/path/to/steam/"
         Condition="'$(SteamFolder)' == '' AND '$(DuckovFolder)' == '' AND '$(ManagedDirectory)' == '' AND '$(ModsDirectory)' == ''" />
</Target>
```

### CI Integration Example

```yaml
# GitHub Actions example
env:
  ManagedDirectory: /opt/game-assemblies/Managed/
  ModsDirectory: /tmp/mod-output/
```

## Scope

### In Scope
- Enable existing `ManagedDirectory` and `ModsDirectory` properties to be overridable via environment variables
- Update property resolution logic in `Directory.Build.props`
- Update validation logic to be CI-aware (skip SteamFolder validation in CI)
- Updated `ResolveSdkPropertiesLib.cs` to preserve explicitly set values
- Updated `ValidateDuckovFolderLib.cs` to relax validation when overrides present
- Documentation updates in README files

### Out of Scope
- Steam Workshop publishing (continues to use existing mechanisms)
- Game assembly downloading/redistribution
- CI workflow templates (can be added later)

## Impact

### Technical Impact
- MSBuild target modifications in `Ducky.Sdk.props`
- Script library updates to reference new properties
- Validation logic changes to allow builds without `DuckovFolder`

### User Impact
- CI environments can build mods using environment variables
- Local development unaffected (backward compatible)
- Developers can specify custom output paths for testing

### Backward Compatibility
- **Fully backward compatible**: Existing mods without new properties continue to work
- Properties are optional; defaults derived from `$(DuckovFolder)` when not set
- No breaking changes to existing workflows

## Alternatives Considered

### Alternative 1: Always Download Assemblies
Rejected because:
- Requires game asset redistribution (licensing concerns)
- Large download overhead for every CI build
- Network dependency

### Alternative 2: Docker Container with Game
Rejected because:
- Heavy image size (~2GB+)
- Complex setup for CI platforms
- Still requires game installation

## Success Criteria

1. CI can build mods without Steam installation using environment variables
2. Existing local development workflows unchanged
3. Sample projects build successfully in CI environment
4. Documentation clearly explains new properties

## Related Changes

None (standalone change)

## Related Specs

- `mod-build` - Extends build system capabilities
- `continuous-integration` - Enables CI builds for mods
