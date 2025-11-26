# Change: Fix Build System Package Resolution and Version Synchronization Issues

## Why
The rebuild_samples.sh script fails with Exit Code 135 during initial version increments due to:
1. Package source resolution conflicts between local and remote package sources
2. Build directory creation race conditions between MSBuild and CSX scripts
3. Version synchronization delays - Sample projects reference versions through nuget.props which don't update immediately during rebuild

## What Changes
- **Fix package source resolution**: Modify Samples/nuget.config to force Ducky.Sdk to use local package source with explicit mapping
- **Fix build directory race condition**: Add explicit obj directory creation in MSBuild targets before script execution
- **Implement direct version management**: Modify Sample project csproj files to use specific Ducky.Sdk versions instead of nuget.props references
- **Add dynamic version updating**: Enhance rebuild_samples.sh script to automatically update project references using `dotnet add package` commands
- **Improve build system reliability**: Ensure consistent behavior between IDE builds and script-based builds

## Impact
- Affected specs: mod-build
- Affected code: Samples/nuget.config, Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.Orchestration.targets, all Sample csproj files, scripts/rebuild_samples.sh
- Resolves: Exit Code 135 failures during rebuild script execution, especially on first-time version increments
- Benefit: Eliminates version synchronization delays and ensures immediate package updates