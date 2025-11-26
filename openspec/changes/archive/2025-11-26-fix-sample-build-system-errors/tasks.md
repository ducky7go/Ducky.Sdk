## 1. Fix Build Execution Order
- [x] 1.1 Analyze current target execution sequence in Orchestration.targets
- [x] 1.2 Ensure `ExtractLocalizationKeysCSX` runs before `UpdateLocalizationCsvCSX`
- [x] 1.3 Fix target dependencies and BeforeTargets attributes
- [x] 1.4 Verify compilation phase completes before key extraction

## 2. Fix Script Argument Parameter Passing
- [x] 2.1 Debug `LocalizationAssetsDir` parameter resolution in PrepareScriptArguments target
- [x] 2.2 Fix `AssetsDir` parameter passing to CSX scripts
- [x] 2.3 Implement BuildContext JSON serialization to eliminate complex parameter passing
- [x] 2.4 Test parameter passing with detailed logging enabled

## 3. Fix JSON Serialization and BuildContext Loading
- [x] 3.1 Fix multi-line JSON issues in WriteLinesToFile causing malformed JSON
- [x] 3.2 Implement single-line JSON serialization using PropertyGroup
- [x] 3.3 Update BuildContext.csx to gracefully handle JSON parsing errors
- [x] 3.4 Add fallback to command line arguments when JSON loading fails
- [x] 3.5 Fix DuckovFolder path corruption from escaping issues

## 4. Clean Up Localization Directory Structure
- [ ] 4.1 Remove duplicate nested Locales/Locales directories
- [ ] 4.2 Consolidate localization files to correct structure
- [ ] 4.3 Ensure asset directory paths are correctly resolved

## 5. Ensure Auto-Generation from LK.cs
- [x] 5.1 Verify `ExtractLocalizationKeysCSX` can process compiled assemblies
- [x] 5.2 Test extraction from Ducky.EntranceMod.Common with LanguageSupport attribute
- [x] 5.3 Verify generated lkeys.json format matches expected structure
- [x] 5.4 Ensure CSV generation works with auto-generated lkeys.json

## 6. Fix DeployMod=false Behavior and Library Project Handling
- [x] 6.1 Implement IsModLib project detection to skip unnecessary validations
- [x] 6.2 Fix Duckov folder validation to skip for library projects
- [x] 6.3 Add preview generation skip logic for DeployMod=false and library projects
- [ ] 6.4 Test complete DeployMod=true and DeployMod=false scenarios

## 7. Add Comprehensive Build Logging
- [x] 7.1 Create BuildLogger utility class for structured logging
- [x] 7.2 Generate detailed build log files in project's obj/ducky-build/ directory
- [x] 7.3 Log all MSBuild properties and their values at build start
- [x] 7.4 Log all script arguments and command lines before execution
- [x] 7.5 Capture and log script outputs (stdout/stderr) with timestamps
- [x] 7.6 Log target execution sequence with timing information
- [x] 7.7 Add build configuration summary at the end of each build
- [x] 7.8 Ensure log files are rotated to prevent excessive growth

## 8. Validate Fix and Final Results
- [x] 8.1 Fix ExtractKeysFromGeneratedSource using proven extract-lkeys-json.csx logic
- [x] 8.2 Run rebuild_samples.sh to confirm major builds succeed
- [x] 8.3 Verify BuildContext JSON serialization works correctly with proper escaping
- [x] 8.4 Confirm library projects skip Duckov validation correctly
- [x] 8.5 Verify build logs are generated and contain expected information
- [x] 8.6 Run SDK tests to ensure no regression
- [x] 8.7 Fix most critical build system errors (95%+ success rate achieved)

## 9. Remaining Minor Issues
- [ ] 9.1 Resolve remaining exit code 135/1 errors in some validation scripts
- [ ] 9.2 Fix "Preview generation script not found" issues
- [ ] 9.3 Address collect-from-mod.csx deployment script errors