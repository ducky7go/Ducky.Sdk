## 1. Create UpdateBuildContextAfterBuildLib.csx Script for Post-Compilation Updates
- [ ] 1.1 Create UpdateBuildContextAfterBuildLib.csx script
  - [ ] Create `Sdk/SDKlibs/scripts/UpdateBuildContextAfterBuildLib.csx` file
  - [ ] Load BuildContext from project directory
  - [ ] Implement dependency detection logic after compilation
  - [ ] Update BuildContext with discovered dependencies
  - [ ] Save updated BuildContext back to JSON file
- [ ] 1.2 Add UpdateBuildContextAfterBuildLib build target
  - [ ] Update `Ducky.Sdk.targets` to add UpdateBuildContextAfterBuildLib target
  - [ ] Ensure UpdateBuildContextAfterBuildLib runs after CoreCompile and before ILRepack
  - [ ] Configure proper dependencies between build targets

## 2. Enhance BuildContext for Post-Compilation Updates
- [ ] 2.1 Add dependency properties to BuildContext class
  - [ ] Add `MainAssemblyPath` property (computed)
  - [ ] Add `DependencyAssemblies` property (List<string>)
  - [ ] Add `HasDependencies` property (computed bool)
  - [ ] Add `ShouldUseILRepack` property (computed based on EnableILRepack and HasDependencies)
- [ ] 2.2 Add update methods to BuildContext
  - [ ] Add `UpdateDependencies()` method for post-compilation updates
  - [ ] Add `SaveToJson()` method to persist updated context
  - [ ] Ensure JSON serialization includes new properties
  - [ ] Add proper logging for update operations

## 3. Update ILRepack Integration
- [ ] 3.1 Modify ILRepackAssembliesLib to use BuildContext dependencies
  - [ ] Remove FindDependencies method from ILRepackAssembliesLib
  - [ ] Update ILRepack execution to use context.DependencyAssemblies
  - [ ] Simplify ILRepack argument building using pre-computed dependencies
  - [ ] Update logging to use BuildContext methods

## 4. Enhance Build Decision Making
- [ ] 4.1 Add dependency-aware build decisions
  - [ ] Implement logic to skip ILRepack when no dependencies exist
  - [ ] Add decision logic for ModDeploy based on dependency presence
  - [ ] Create utility methods for common dependency patterns
- [ ] 4.2 Update build orchestration scripts
  - [ ] Modify build targets to check context.ShouldUseILRepack
  - [ ] Add conditional deployment logic based on dependencies
  - [ ] Update build logging to show dependency information

## 5. Testing and Validation
- [ ] 5.1 Create test scenarios
  - [ ] Test mod with no external dependencies
  - [ ] Test mod with multiple dependencies
  - [ ] Test ILRepack enable/disable scenarios
  - [ ] Test deployment scenarios with and without dependencies
- [ ] 5.2 Validate build performance
  - [ ] Measure build time improvements from single dependency scan
  - [ ] Verify dependency information consistency across build phases
  - [ ] Test edge cases (missing assemblies, circular dependencies)
  - [ ] Verify Lib step runs at correct time in build pipeline

## 6. Documentation and Migration
- [ ] 6.1 Update BuildContext documentation
  - [ ] Document new dependency properties
  - [ ] Add examples of dependency-aware build decisions
  - [ ] Update BuildContext JSON schema if needed
  - [ ] Document Lib step purpose and timing in build pipeline
- [ ] 6.2 Create migration guide
  - [ ] Document changes for custom build scripts
  - [ ] Provide examples of using new dependency properties
  - [ ] Update SDK samples to demonstrate best practices
  - [ ] Explain Lib.csx script usage and extension points