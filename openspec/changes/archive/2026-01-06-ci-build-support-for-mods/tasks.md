# Implementation Tasks

## Overview
Ordered list of implementation tasks for CI build support feature.

## Tasks

### Phase 1: MSBuild Property Updates

- [x] **1.1 Update Directory.Build.props property override logic**
  - Modify `ManagedDirectory` property to preserve explicitly set values
  - Modify `ModsDirectory` property to preserve explicitly set values
  - Ensure explicit values override DuckovFolder-derived values
  - **File**: `Sdk/Directory.Build.props`

- [x] **1.2 Update validation targets for CI awareness**
  - Modify `FailIfSteamFolderMissing` target to check for explicit `ManagedDirectory` and `ModsDirectory`
  - Add condition `Condition="'$(CI)' != 'true'"` to skip validation in CI
  - Update error message to mention override options
  - **File**: `Sdk/Directory.Build.props`

- [x] **1.3 Update Ducky.Sdk.props for consistency**
  - Ensure property override logic is consistent across all SDK props files
  - **File**: `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.props`

### Phase 2: Script Library Updates

- [x] **2.1 Update ResolveSdkPropertiesLib.cs**
  - Preserve explicitly set `ManagedDirectory` value (don't override if set)
  - Preserve explicitly set `ModsDirectory` value (don't override if set)
  - Update logging to show whether using explicit or derived values
  - **File**: `Sdk/SDKlibs/scripts/libs/ResolveSdkPropertiesLib.cs`

- [x] **2.2 Update ValidateDuckovFolderLib.cs**
  - Relax validation when `ManagedDirectory` and `ModsDirectory` are explicitly set
  - Allow builds to succeed without `DuckovFolder` when paths are explicitly set
  - Add CI environment detection (check for `CI` environment variable)
  - Update error messages to be more informative about override options
  - **File**: `Sdk/SDKlibs/scripts/libs/ValidateDuckovFolderLib.cs`

### Phase 3: Testing and Validation

- [x] **3.1 Update test project configurations**
  - Add test configurations using `ManagedDirectory` and `ModsDirectory` properties
  - Verify backward compatibility tests still pass
  - **Files**: `Sdk/Tests/Ducky.Sdk.Lib.Tests/Ducky.Sdk.Lib.Tests.csproj`

- [ ] **3.2 Create CI build test scenario**
  - Test build with `ManagedDirectory` and `ModsDirectory` environment variables (no DuckovFolder)
  - Verify mod artifacts are produced in correct location
  - Test on Windows, Linux, macOS
  - **Validation**: Manual CI workflow test

### Phase 4: Documentation

- [x] **4.1 Update README.md**
  - Document `ManagedDirectory` and `ModsDirectory` override capability
  - Add CI environment setup instructions
  - Include environment variable examples for GitHub Actions
  - **Files**: `README.md`, `README_en.md`

- [x] **4.2 Update AGENTS.md**
  - Document property override behavior in build system reference
  - Update property resolution documentation
  - **File**: `openspec/AGENTS.md`

### Phase 5: Sample Projects

- [x] **5.1 Verify sample projects build**
  - Test existing samples still build locally with DuckovFolder
  - Ensure no breaking changes
  - **Files**: All `Samples/*` projects

## Dependencies

- Phase 1 must complete before Phase 2 (MSBuild properties must be overridable first)
- Phase 2 can run in parallel for all script libraries
- Phase 3 and 4 can run in parallel
- Phase 5 depends on Phase 1-2 completion

## Validation Checklist

- [x] Local builds with DuckovFolder still work (backward compatibility)
- [x] CI builds with environment variables work (new functionality)
- [x] All existing tests pass
- [x] Sample projects build successfully
- [x] Documentation is clear and complete
- [x] No regression in Steam Workshop publishing
