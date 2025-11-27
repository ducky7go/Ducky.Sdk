# Change: Optimize Build Dependency Resolution

## Why
Currently, dependency detection and resolution happens at different stages of the build process, leading to:
- `FindDependencies` being called multiple times during ILRepack processing
- ModDeploy needing to understand whether dependencies exist to decide between copying main assembly only vs all dependencies
- Duplicate dependency scanning logic across build steps
- Inconsistent dependency information between ILRepack and ModDeploy phases

By computing dependency information once in BuildContext and making it available throughout the build pipeline, we can:
- Eliminate duplicate dependency scanning
- Provide consistent dependency information to all build steps
- Enable smarter deployment decisions based on dependency presence
- Improve build performance by scanning dependencies only once

## What Changes
- Add a new UpdateBuildContextAfterBuildLib build step that runs immediately after CoreCompile
- Create UpdateBuildContextAfterBuildLib.csx script to update BuildContext with post-compilation information
- Add dependency resolution properties and update methods to BuildContext
- Move `FindDependencies` logic from ILRepackAssembliesLib to UpdateBuildContextAfterBuildLib step
- Add computed properties for dependency-aware build decisions
- Update ILRepack to use pre-computed dependencies from BuildContext
- Enable ModDeploy to make intelligent copying decisions based on dependency information
- Ensure BuildContext is updated and re-saved after UpdateBuildContextAfterBuildLib step completion

## Impact
- **Affected specs**: mod-build (optimizes build process, adds dependency resolution capabilities)
- **Affected code**:
  - `Sdk/SDKlibs/scripts/shared/BuildContext.cs` - add dependency detection logic and update methods
  - `Sdk/SDKlibs/scripts/UpdateBuildContextAfterBuildLib.csx` - NEW: post-compilation BuildContext update script
  - `Sdk/SDKlibs/scripts/libs/ILRepackAssembliesLib.cs` - use BuildContext dependencies instead of FindDependencies
  - `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.targets` - add UpdateBuildContextAfterBuildLib target after CoreCompile
  - Build orchestration scripts - leverage dependency information for smarter decisions
- **Performance improvements**: Single dependency scan instead of multiple scans
- **New capabilities**: BuildContext provides comprehensive dependency information for all build phases
- **Build flow change**: New UpdateBuildContextAfterBuildLib step between CoreCompile and subsequent build phases