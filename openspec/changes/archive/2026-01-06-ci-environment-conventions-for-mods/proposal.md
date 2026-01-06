# CI Environment Conventions for Mods

## Overview

Enable the SDK to automatically apply default path conventions for `ManagedDirectory` and `ModsDirectory` when building in CI environments, without requiring users to explicitly configure environment variables in their workflows. Paths shall be relative to the Git repository root for consistency across different project structures.

## Background

The CI build support for mods was implemented in `2026-01-06-ci-build-support-for-mods`, which allows `ManagedDirectory` and `ModsDirectory` to be overridden via environment variables. However, users still need to explicitly set these environment variables in their CI workflows. For a better developer experience, the SDK should automatically apply sensible defaults when building in CI environments.

Using Git root as the base path ensures consistency regardless of where the project is located within the repository structure (e.g., `src/MyMod`, `samples/MyMod`, or repository root).

## Problem Statement

1. **Manual Configuration Required**: Users must explicitly set `ManagedDirectory` and `ModsDirectory` environment variables in CI workflows
2. **No Default CI Behavior**: The SDK doesn't provide automatic defaults for CI environments
3. **Poor Developer Experience**: Every project referencing the SDK needs to configure the same environment variables
4. **Path Inconsistency**: Using project-relative paths can cause issues with multi-project repositories

## Proposed Solution

### SDK-Level Automatic CI Defaults Based on Git Root

When the SDK detects a CI environment (via the `CI` environment variable), it shall automatically apply these default values:

| Property | CI Default Value | Description |
|----------|------------------|-------------|
| `ManagedDirectory` | `$(GitRoot)/Managed` | Game assemblies directory at Git repository root |
| `ModsDirectory` | `$(GitRoot)/artifacts/Mods` | Mod deployment output at Git repository root |

### Git Root Detection

The SDK shall detect the Git repository root using MSBuild's ability to find the `.git` directory by traversing upward from the project directory:

```xml
<PropertyGroup>
  <!-- Find Git root by looking for .git directory -->
  <GitRootPath Condition=" '$(GitRootPath)' == '' ">$([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)../..'))</GitRootPath>
  <GitRootPath Condition=" '$(GitRootPath)' == '' AND Exists('$(MSBuildProjectDirectory)/../.git') ">$([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)/..'))</GitRootPath>
  <GitRootPath Condition=" '$(GitRootPath)' == '' AND Exists('$(MSBuildProjectDirectory)/../../.git') ">$([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)/../..'))</GitRootPath>
  <GitRootPath Condition=" '$(GitRootPath)' == '' AND Exists('$(MSBuildProjectDirectory)/../../../.git') ">$([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)/../../..'))</GitRootPath>
  <!-- Fallback to project directory if .git not found -->
  <GitRootPath Condition=" '$(GitRootPath)' == '' ">$(MSBuildProjectDirectory)</GitRootPath>
</PropertyGroup>
```

### Implementation Location

Modify `InitializeDuckySdkProperties` target in `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.props`:

```xml
<Target Name="InitializeDuckySdkProperties" BeforeTargets="BeforeBuild;BeforeResolveReferences">
  <PropertyGroup>
    <!-- Detect Git root for CI defaults -->
    <GitRootPath Condition=" '$(GitRootPath)' == '' AND Exists('$(MSBuildProjectDirectory)/../.git') ">
      $([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)/..'))
    </GitRootPath>
    <GitRootPath Condition=" '$(GitRootPath)' == '' AND Exists('$(MSBuildProjectDirectory)/../../.git') ">
      $([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)/../..'))
    </GitRootPath>
    <GitRootPath Condition=" '$(GitRootPath)' == '' AND Exists('$(MSBuildProjectDirectory)/../../../.git') ">
      $([System.IO.Path]::GetFullPath('$(MSBuildProjectDirectory)/../../..'))
    </GitRootPath>
    <GitRootPath Condition=" '$(GitRootPath)' == '' ">$(MSBuildProjectDirectory)</GitRootPath>

    <!-- CI environment: apply automatic defaults if not explicitly set -->
    <ManagedDirectory Condition=" '$(CI)' == 'true' AND '$(ManagedDirectory)' == '' AND '$(DuckovFolder)' == '' ">
      $(GitRootPath)/Managed
    </ManagedDirectory>
    <ModsDirectory Condition=" '$(CI)' == 'true' AND '$(ModsDirectory)' == '' AND '$(DuckovFolder)' == '' ">
      $(GitRootPath)/artifacts/Mods
    </ModsDirectory>

    <!-- Fallback to DuckovFolder for non-CI or when explicitly set -->
    <ManagedDirectory Condition=" '$(ManagedDirectory)' == '' AND '$(DuckovFolder)' != '' ">
      $(DuckovFolder)Duckov_Data/Managed/
    </ManagedDirectory>
    <ModsDirectory Condition=" '$(ModsDirectory)' == '' AND '$(DuckovFolder)' != '' ">
      $(DuckovFolder)Duckov_Data/Mods/
    </ModsDirectory>
  </PropertyGroup>
</Target>
```

### Property Resolution Priority

1. **Explicit user value** (via environment variable or MSBuild property) - highest priority
2. **CI environment default** (when `CI=true`, no DuckovFolder, based on Git root)
3. **DuckovFolder-derived value** (for local development)

### Directory Structure Examples

```
repository/                          # Git root
├── .git/
├── Managed/                         # CI default: game assemblies
│   ├── Assembly-CSharp.dll
│   └── ...
├── artifacts/                       # CI default: build outputs
│   └── Mods/
│       └── MyMod.dll
├── src/
│   └── MyMod/                       # Project location (any depth)
│       └── MyMod.csproj
└── samples/
    └── AnotherMod/                  # Another project
        └── AnotherMod.csproj
```

Both `src/MyMod` and `samples/AnotherMod` will use the same `Managed/` and `artifacts/Mods/` directories at Git root when building in CI.

## Scope

### In Scope
- Modify `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.props` to add Git root detection and CI-aware default values
- Update `continuous-integration` spec with new CI default behavior
- Add `.gitignore` rules for `Managed/` and `artifacts/` directories (if not already present)

### Out of Scope
- Modifying CI workflow files (SDK handles defaults automatically)
- Changes to local development workflow behavior
- Modifying `ResolveSdkPropertiesLib.cs` or other script libraries

## Impact

### Technical Impact
- Update `Ducky.Sdk.props` `InitializeDuckySdkProperties` target with Git root detection and CI logic
- No changes required to CI workflows - SDK applies defaults automatically
- Git is typically available in CI environments, making this approach reliable

### User Impact
- Projects referencing the SDK build in CI without any configuration
- Works consistently regardless of project location within repository
- Users can still override defaults by explicitly setting environment variables
- Local development remains unaffected (backward compatible)

### Backward Compatibility
- **Fully backward compatible**: CI defaults only apply when `CI=true` and no explicit values are set
- Existing workflows with explicit environment variables continue to work unchanged
- Local development workflow unchanged

## Success Criteria

1. A project referencing Ducky.Sdk builds successfully in CI without any environment variable configuration
2. CI artifacts are output to `$(GitRoot)/artifacts/Mods` directory
3. Projects at any directory depth within the repository use the same Git root-based paths
4. Users can override defaults by explicitly setting environment variables
5. Local development builds work exactly as before

## Related Changes

- `2026-01-06-ci-build-support-for-mods` (archived) - Implemented the CI build path override capability

## Related Specs

- `continuous-integration` - CI/CD build and publishing capabilities
- `mod-build` - Mod development build system
