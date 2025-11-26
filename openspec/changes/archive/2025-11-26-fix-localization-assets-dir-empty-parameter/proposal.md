# Change: Fix LocalizationAssetsDir Multi-Path Formatting

## Why
The LocalizationAssetsDir property in Ducky.EntranceMod.Common.csproj uses incorrect spacing around semicolon separators, which creates empty string entries when MSBuild parses the multi-path value.

## What Changes
- Fix semicolon separator formatting: remove spaces after semicolons in LocalizationAssetsDir
- Ensure clean MSBuild semicolon-separated path format: `path1;path2` instead of `path1; path2`
- **BREAKING**: None (fixes malformed configuration)

## Impact
- Affected specs: mod-build
- Affected code: Samples/Ducky.EntranceMod.Common/Ducky.EntranceMod.Common.csproj:14