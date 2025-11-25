# Change: Fix LoadConfig Default Value Validation Issue

## Why
When `LoadConfig<T>` method encounters a missing key, it directly saves the provided `defaultValue` without going through the basic type validation that would normally occur during the save process. This can lead to invalid data being persisted, especially for complex types or when the `defaultValue` doesn't match expected constraints.

## What Changes
- Add validation for default values before saving them when a key is missing
- Ensure type consistency and data integrity for all supported types
- Maintain backward compatibility while fixing the validation gap

## Impact
- Affected specs: mod-options
- Affected code: Sdk/SDKlibs/Ducky.Sdk.Lib/Options/ModOptions.cs:291-349
- Requires additional unit tests to verify validation behavior