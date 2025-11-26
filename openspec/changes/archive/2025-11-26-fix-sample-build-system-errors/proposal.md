# Change: Fix Sample Build System Errors

## Why
The sample projects are failing to build due to missing localization files, validation script failures, and inconsistent file structure, preventing developers from building and testing the SDK samples. Root cause analysis reveals the issue is NOT missing manual file creation, but rather **build sequence and parameter passing problems**.

## What Changes
- Fix build execution order: Ensure `ExtractLocalizationKeysCSX` runs **before** `UpdateLocalizationCsvCSX`
- Resolve validation script execution issues in build environment (working directory vs manual execution)
- Fix script argument parameter passing for `LocalizationAssetsDir` and `AssetsDir`
- Clean up duplicate nested localization directories (Locales/Locales/)
- Ensure `lkeys.json` is auto-generated from `LK.cs` via proper build sequencing
- **Fix DeployMod=false behavior**: When `DeployMod=false`, clean mod files from game directory instead of skipping entirely
- **Add comprehensive build logging**: Generate detailed build logs in project's obj folder including all properties, parameters, and script outputs
- **BREAKING**: None - these are fixes to restore intended functionality

## Build Sequence Analysis (Critical for Understanding)

### Current Problem Sequence:
1. `ExtractLocalizationKeysCSX` runs **first** ✅ **BUT**
2. Script receives **NO ARGUMENTS** → `dotnet script extract-localization-keys-enhanced.csx` (without parameters) ❌
3. Script fails with exit code 1 → No `lkeys.json` generated ❌
4. `UpdateLocalizationCsvCSX` runs **second** → Looks for `lkeys.json` → File not found → Build fails ❌
5. Validation scripts fail due to working directory context issues

### Correct Expected Sequence:
1. **Build Compilation** → `LK.cs` compiled into assembly
2. `ExtractLocalizationKeysCSX` → Runs **with proper arguments** → Generates `lkeys.json` from assembly ✅
3. `UpdateLocalizationCsvCSX` → Uses generated `lkeys.json` to update CSV files ✅

### Key Parameter Issues:
- `LocalizationAssetsDir`: Empty/incorrect parameter passed to scripts
- `AssetsDir`: Path resolution problems in build context vs manual execution
- Working directory: Scripts expect project directory but run from different contexts

## Impact
- Affected specs: mod-build, continuous-integration
- Affected code: Build orchestration targets, script argument preparation, validation scripts
- Fixes: Samples can be built successfully with `rebuild_samples.sh`, proper build sequencing restored