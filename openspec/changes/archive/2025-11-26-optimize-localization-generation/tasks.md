# Localization Generation Optimization Tasks

## Ordered Work Items

### 1. Research and Analysis
- [x] Analyze current `UpdateLocalesCsv` target implementation in Ducky.Sdk.targets
- [x] Examine existing localization asset generation scripts (likely in scripts/ directory)
- [x] Review how `LocalizationAssetsDir` property is currently processed
- [x] Identify existing copy/distribution mechanisms in the build pipeline

### 2. Simplified LocalizationAssetsDir Logic Implementation
- [x] Implement logic to use local `assets/` folder as primary generation location
- [x] Remove dependency on `IsModLib` property for optimization detection
- [x] Create function to detect multi-directory scenarios via `LocalizationAssetsDir` property
- [x] Add MSBuild properties to handle primary and target localization paths

### 3. Centralized Generation Logic
- [x] Modify existing localization generation scripts to support single-location generation
- [x] Update CSV file generation to work with primary location only
- [x] Update translation file generation to work with primary location only
- [x] Ensure all localization processing is redirected to primary location

### 4. Asset Copy Distribution System
- [x] Create new MSBuild target `SimpleCopyLocalizationAssets` using system commands
- [x] Implement reliable file copying using Unix commands (`cp`, `rsync`)
- [x] Copy all file types: `lkeys.json`, CSV files, language directories, hash files
- [x] Preserve complete directory structure including language subdirectories

### 5. Validation and Consistency Checks
- [x] Implement file set verification across all localization directories
- [x] Create directory structure validation logic
- [x] Add validation for missing or extra files in target directories
- [x] **Implement error reporting for validation failures**

### 6. MSBuild Target Integration
- [x] Modify `UpdateLocalesCsv` target to orchestrate centralized generation
- [x] Integrate `SimpleCopyLocalizationAssets` target into build pipeline
- [x] Ensure proper target dependencies and execution order
- [x] Add target conditionals for single vs multiple directory scenarios
- [x] Fix XML syntax issues in MSBuild targets

### 7. Backward Compatibility Assurance
- [x] Test single directory scenarios to ensure unchanged behavior
- [x] Verify existing multi-directory projects produce identical results
- [x] Ensure no breaking changes to existing project configurations
- [x] Validate that all existing MSBuild properties work as before

### 8. Testing and Validation
- [x] Create test projects with various `LocalizationAssetsDir` configurations
- [x] Test `LocalizationAssetsDir`-based optimization detection
- [x] Verify asset copying works correctly for all file types (CSV, MD, JSON, hash files)
- [x] Test complete file copying including language subdirectories
- [x] Validate successful copying to all target directories

### 9. Documentation Updates
- [ ] Update SDK documentation to explain the optimization behavior
- [ ] Add examples of multi-directory localization setup
- [ ] Document LocalizationAssetsDir-based optimization rules
- [ ] Update troubleshooting guide for localization issues

### 10. Performance Benchmarking
- [ ] Measure build time improvements for typical multi-directory scenarios
- [ ] Compare file I/O operations before and after optimization
- [ ] Validate that single-directory performance is unchanged
- [ ] **Document performance benefits in release notes**

### 11. Global Using Dependency Fixes (Bonus)
- [x] Fix SodaLocalization and TeamSoda global using issues
- [x] Add conditional global using based on game dependency availability
- [x] Ensure clean builds when game dependencies are missing
- [x] Maintain compatibility for projects with full game dependencies

## Dependencies

### Parallelizable Work
- Tasks 1, 2, 7 can be done in parallel
- Task 8 can be partially parallelized once components are ready

### Sequential Dependencies
- Task 3 depends on Task 2 completion
- Task 4 depends on Task 3 completion
- Task 5 depends on Task 4 completion
- Task 6 depends on Tasks 2-5 completion
- Task 9 depends on Task 6 completion
- Task 10 depends on Task 8 completion

## Validation Criteria

- All existing tests continue to pass
- New tests verify centralized generation works correctly
- Build time measurably improves for multi-directory setups
- No regressions in single-directory scenarios
- Asset consistency is maintained across all directories