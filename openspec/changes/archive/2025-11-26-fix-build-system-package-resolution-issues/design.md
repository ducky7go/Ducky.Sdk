## Context

The Ducky.Sdk build system uses CSX scripts executed through MSBuild targets for various build automation tasks. The rebuild_samples.sh script automates the process of building the SDK and rebuilding all sample projects to validate changes.

Current architecture uses nuget.props to define LocalNuGetVersion property, which Sample projects reference through PackageReference. This approach has timing issues where updated package versions are not immediately available during rebuild scripts.

## Goals / Non-Goals

- **Goals**:
  - Fix Exit Code 135 failures in rebuild_samples.sh script
  - Ensure consistent build behavior between IDE and script-based builds
  - Resolve package source conflicts for Ducky.Sdk
  - Eliminate race conditions in build directory creation
  - Implement immediate version synchronization between SDK packaging and sample project updates
  - Use direct package management commands for reliable version updates

- **Non-Goals**:
  - Major architectural changes to build system
  - Performance optimizations beyond fixing the immediate issues
  - Changes to CSX script functionality
  - Breaking changes to existing project structure beyond version management

## Decisions

- **Decision**: Use explicit package source mapping instead of relying on default NuGet behavior
  - **Reasoning**: Ensures Ducky.Sdk always resolves to local version during development
  - **Alternatives considered**: Using version constraints only, modifying project files directly

- **Decision**: Add explicit obj directory creation before script execution
  - **Reasoning**: Eliminates race condition between MSBuild and CSX scripts for directory creation
  - **Alternatives considered**: Adding retry logic in scripts, modifying script error handling

- **Decision**: Replace nuget.props-based version references with direct csproj version specification
  - **Reasoning**: Eliminates version synchronization delays and ensures immediate package updates
  - **Alternatives considered**: Using MSBuild property inheritance, dynamic version resolution during build

- **Decision**: Use `dotnet add package` commands for dynamic version updates in rebuild script
  - **Reasoning**: Leverages NuGet's native package management for reliable version updates
  - **Alternatives considered**: Direct csproj file modification, MSBuild property updates

## Risks / Trade-offs

- **Risk**: Package source mapping may affect external contributors' ability to build from source
  - **Mitigation**: Clear documentation and fallback to nuget.org for non-Ducky.Sdk packages

- **Risk**: Additional MSBuild targets may slightly increase build time
  - **Mitigation**: Conditional directory creation only when needed, minimal overhead

- **Trade-off**: More explicit configuration vs. more "magical" default behavior
  - **Justification**: Explicitness reduces ambiguity and debugging complexity

## Migration Plan

1. Update Samples/nuget.config with package source mapping
2. Modify Ducky.Sdk.Orchestration.targets to add directory creation
3. Test rebuild_samples.sh with clean environment
4. Validate IDE builds continue working
5. Deploy updated SDK package

## Open Questions

- Should we consider making the package source mapping configurable for different environments?
- Do we need additional safeguards for concurrent build processes?
- Should we add more comprehensive error handling for package source resolution failures?