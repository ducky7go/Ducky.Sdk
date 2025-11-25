# ModOptions Collection Serialization Specification

## ADDED Requirements

### Requirement: Collection Type JSON Serialization
ModOptions SHALL serialize collection types to JSON format for proper storage and retrieval.

#### Scenario: List<string> Save and Load
- **WHEN** a developer calls `SaveConfig` with a `List<string>` value
- **AND** the `List<string>` contains valid string data
- **THEN** the data SHALL be serialized to JSON format before storage
- **AND** `LoadConfig<List<string>>` SHALL deserialize the JSON back to the original list

#### Scenario: Array Type Save and Load
- **WHEN** a developer calls `SaveConfig` with an array type (e.g., `string[]`)
- **AND** the array contains valid data
- **THEN** the data SHALL be serialized to JSON format before storage
- **AND** `LoadConfig<string[]>()` SHALL deserialize the JSON back to the original array

#### Scenario: Dictionary Type Save and Load
- **WHEN** a developer calls `SaveConfig` with a `Dictionary<string, T>` value
- **AND** the dictionary contains valid key-value pairs
- **THEN** the data SHALL be serialized to JSON format before storage
- **AND** `LoadConfig<Dictionary<string, T>>()` SHALL deserialize the JSON back to the original dictionary

### Requirement: Empty Collection Handling
ModOptions SHALL properly handle empty collections during serialization and deserialization.

#### Scenario: Empty List<string>
- **WHEN** a developer calls `SaveConfig` with an empty `List<string>`
- **THEN** the empty list SHALL be serialized as `"[]"` in JSON
- **AND** `LoadConfig<List<string>>` SHALL return an empty list
- **AND** the result SHALL NOT be null

#### Scenario: Null Collection Handling
- **WHEN** a developer calls `SaveConfig` with a null collection reference
- **THEN** null SHALL be stored directly (not as JSON)
- **AND** `LoadConfig` SHALL return null for nullable collection types

### Requirement: Special Character Handling in Collections
ModOptions SHALL properly escape special characters in collection string data.

#### Scenario: List with Special Characters
- **WHEN** a `List<string>` contains strings with quotes, newlines, or Unicode characters
- **THEN** these SHALL be properly escaped in the JSON representation
- **AND** `LoadConfig<List<string>>` SHALL return the original strings unchanged

### Requirement: Backward Compatibility for Simple Types
ModOptions SHALL maintain existing behavior for all simple and primitive types.

#### Scenario: Simple Type Preservation
- **WHEN** saving and loading primitive types (int, string, bool, etc.)
- **THEN** the behavior SHALL remain exactly as before
- **AND** these types SHALL NOT be JSON serialized

#### Scenario: DateTime Type Preservation
- **WHEN** saving and loading DateTime/DateTimeOffset types
- **THEN** the Unix timestamp conversion behavior SHALL remain unchanged
- **AND** these types SHALL continue to be stored as long values

## MODIFIED Requirements

### Requirement: Simple Type Classification
The `IsSimpleType()` method SHALL correctly identify collection types as complex types requiring JSON serialization.

#### Scenario: Generic Collection Detection
- **WHEN** `IsSimpleType()` is called with `List<T>`, `Dictionary<TKey, TValue>`, or other generic collection types
- **THEN** it SHALL return `false` to indicate JSON serialization is required

#### Scenario: Array Type Detection
- **WHEN** `IsSimpleType()` is called with array types (`T[]`)
- **THEN** it SHALL return `false` to indicate JSON serialization is required

#### Scenario: Simple Type Detection (Unchanged)
- **WHEN** `IsSimpleType()` is called with primitive types, enums, string, decimal, DateTime, etc.
- **THEN** it SHALL return `true` as before

## Implementation Details

### Collection Type Detection Logic
```csharp
private static bool IsSimpleType(Type t)
{
    if (t == null) return false;

    // Handle nullable types
    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
    {
        var underlyingType = Nullable.GetUnderlyingType(t);
        return underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset);
    }

    // Collection types are not simple types (require JSON serialization)
    if (t.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(t))
        return false;

    // Array types are not simple types
    if (t.IsArray)
        return false;

    // Original simple type checks
    return t.IsPrimitive
           || t.IsEnum
           || t == typeof(string)
           || t == typeof(decimal)
           || t == typeof(DateTime)
           || t == typeof(DateTimeOffset)
           || t == typeof(TimeSpan)
           || t == typeof(Guid);
}
```

### Test Validation Requirements
- All existing tests must continue to pass
- New tests must verify JSON serialization for collections
- Storage layer should store collections as JSON strings
- Performance impact should be minimal for simple types