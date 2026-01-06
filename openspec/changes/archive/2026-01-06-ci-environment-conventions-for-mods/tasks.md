# Implementation Tasks

## 1. SDK Implementation

- [x] 1.1 Modify `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.props`
  - Add Git root detection logic in `InitializeDuckySdkProperties` target
  - Detect `.git` directory by traversing upward from project directory (up to 3 levels)
  - Add fallback to project directory if `.git` not found
  - Add `ManagedDirectory` default for CI: `$(GitRootPath)/Managed`
  - Add `ModsDirectory` default for CI: `$(GitRootPath)/artifacts/Mods`
  - Ensure proper property resolution priority: explicit > CI default > DuckovFolder

- [x] 1.2 Test Git root detection logic
  - Test with project at repository root
  - Test with project in `src/` subdirectory
  - Test with project in `samples/` subdirectory
  - Test with project in deeper nesting (`src/features/MyMod`)

## 2. Git Configuration

- [x] 2.1 Update `.gitignore` to exclude CI-generated directories
  - Add `Managed/` to `.gitignore` (if not already present)
  - Add `artifacts/` to `.gitignore` (if not already present)

## 3. Validation

- [x] 3.1 Run `openspec validate ci-environment-conventions-for-mods --strict`
  - Ensure all spec files are valid
  - Fix any validation errors before proceeding

- [x] 3.2 Test SDK CI defaults end-to-end
  - Create a test project referencing Ducky.Sdk
  - Build in CI environment without setting environment variables
  - Verify `ManagedDirectory` defaults to `$(GitRoot)/Managed`
  - Verify `ModsDirectory` defaults to `$(GitRoot)/artifacts/Mods`
  - Confirm build succeeds and artifacts are in expected locations

- [x] 3.3 Test override behavior
  - Set explicit environment variables and verify they take precedence
  - Set `DuckovFolder` in CI and verify it takes precedence over defaults
  - Verify local development builds work unchanged

- [x] 3.4 Test multi-project repository
  - Create multiple projects at different depths in the repository
  - Verify all projects use the same Git root-based paths
  - Confirm artifacts from all projects go to the same `$(GitRoot)/artifacts/Mods`
