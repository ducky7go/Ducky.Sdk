# Change: Reduce ILRepack Verbosity

## Why
The current ILRepack command uses both `/log` and `/verbose` flags, producing excessive log output that clutters the build output and makes it difficult to identify important information.

## What Changes
- Remove `/verbose` flag from ILRepack command to reduce log noise
- Keep `/log` flag for essential error and warning information
- Add optional configuration property for verbose mode if users need detailed output
- **BREAKING**: None (log output improvement only)

## Impact
- Affected specs: mod-packaging
- Affected code: `Sdk/SDKlibs/Ducky.Sdk/Ducky.Sdk.Packaging.targets` (ILRepack Exec command)
- Backwards compatible: Yes
- User experience: Cleaner build output