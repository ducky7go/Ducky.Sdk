# Fix List<string> JSON Serialization in ModOptions

## Why

Currently, when developers try to save and load `List<string>` or other collection types using ModOptions, the data is not properly serialized to JSON. The `IsSimpleType()` method incorrectly classifies collection types as simple types, leading to direct storage instead of JSON serialization. This causes data corruption or runtime exceptions when trying to load the saved data.

## Summary

Fix the issue where `LoadConfig` fails to properly handle `List<string>` types because they are not being serialized to JSON during `SaveConfig`. The current `IsSimpleType()` method incorrectly treats collections as simple types, causing direct storage instead of JSON serialization.

## Problem

Currently, when using `SaveConfig` with a `List<string>`, the data is stored directly without JSON serialization because `List<string>` is not recognized as a complex type. This leads to deserialization failures or incorrect behavior when `LoadConfig` tries to retrieve the data.

## Root Cause Analysis

The `IsSimpleType()` method in `ModOptions.cs` only checks for primitive types, enums, and basic value types. It doesn't account for collection types like:
- `List<T>`
- `T[]`
- `Dictionary<TKey, TValue>`
- Other IEnumerable collections

These should be treated as complex types and serialized to JSON.

## What Changes

### Core Code Changes
- **Modified `IsSimpleType()` method in `ModOptions.cs`**: Added collection type detection to return `false` for generic collections and arrays, ensuring they get JSON serialized instead of directly stored.

### New Unit Tests
Added 6 comprehensive test methods to `ModOptionsTests.cs`:
- `SaveAndLoadListString_ShouldConvertToJson()` - Basic List<string> JSON serialization
- `SaveAndLoadEmptyListString_ShouldWork()` - Empty list handling
- `SaveAndLoadListStringWithSpecialCharacters_ShouldWork()` - Special character and Unicode support
- `SaveAndLoadArrayTypes_ShouldConvertToJson()` - Array type serialization
- `VerifyListStringStoredAsJsonString_ShouldPass()` - Verification of JSON storage format
- `SaveAndLoadDictionary_ShouldConvertToJson()` - Dictionary type serialization

## Solution

Modify the `IsSimpleType()` method to:
1. Explicitly return `false` for generic collection types
2. Return `false` for array types
3. Maintain existing behavior for actual simple types

## Impact

- **Backward Compatibility**: Existing configurations using primitive types remain unaffected
- **New Functionality**: Complex collection types will be properly serialized/deserialized
- **Performance**: Minimal impact - only affects collection type handling

## Test Plan

Add comprehensive unit tests to `ModOptionsTests.cs`:
- `List<string>` save/load scenarios
- Empty lists
- Lists with special characters
- Other collection types (arrays, dictionaries)
- Verify JSON storage format in underlying storage

## Alternatives Considered

1. **Type-specific handling**: Add specific checks for each collection type
   - More verbose but explicit
   - Harder to extend for new collection types

2. **Whitelist approach**: Only serialize known complex types
   - Safer but requires maintenance for new types
   - Current approach already uses blacklist for simple types

The chosen solution modifies the existing `IsSimpleType()` logic to be more inclusive of collection types while maintaining the existing architecture.