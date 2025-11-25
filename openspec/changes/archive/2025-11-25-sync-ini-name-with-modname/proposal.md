# Change: Sync INI name with ModName automatically

## Why
Ensure consistency between the ModName property in MSBuild and the name field in info.ini files, preventing configuration mismatches that can occur when developers manually modify INI files or when INI files are generated with different names than the actual ModName.

## What Changes
- Add name synchronization functionality to ensure-info-ini.csx script
- Create new mod-build specification to cover INI file generation and synchronization
- Automatically replace INI name field with ModName when they differ
- Maintain backward compatibility with existing manual INI configurations

## Impact
- Affected specs: mod-build (new specification for INI processing)
- Affected code:
  - `Sdk/SDKlibs/Ducky.Sdk/scripts/ensure-info-ini.csx` - Add name validation and replacement
  - `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.Assets.targets` - Update EnsureInfoIni target to support name synchronization