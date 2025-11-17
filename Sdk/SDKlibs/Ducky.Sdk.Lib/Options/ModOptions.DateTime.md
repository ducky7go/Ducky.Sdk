# DateTime and DateTimeOffset Handling in ModOptions

## Overview

ModOptions provides automatic Unix timestamp-based serialization for `DateTime`, `DateTimeOffset`, and their nullable variants (`DateTime?` and `DateTimeOffset?`).

## Motivation

Storing date/time values in their native format can lead to:
- Time zone conversion issues
- Serialization format inconsistencies across platforms
- Parsing errors when loading data

By standardizing on Unix timestamps (seconds since 1970-01-01 00:00:00 UTC), we ensure:
- Consistent cross-platform behavior
- Compact storage format
- Easy interoperability with other systems
- Clear semantics for null values

## Usage

### Basic DateTime Storage

```csharp
using Ducky.Sdk.Options;

// Save a DateTime
var timestamp = DateTime.UtcNow;
ModOptions.ForName.SaveConfig("lastSaveTime", timestamp);

// Load the DateTime
var loaded = ModOptions.ForName.LoadConfig<DateTime>("lastSaveTime");
```

### DateTimeOffset Storage

```csharp
// Save a DateTimeOffset (preserves UTC offset information)
var offset = DateTimeOffset.UtcNow;
ModOptions.ForName.SaveConfig("eventTime", offset);

// Load the DateTimeOffset
var loadedOffset = ModOptions.ForName.LoadConfig<DateTimeOffset>("eventTime");
```

### Nullable DateTime Types

```csharp
// Save a nullable DateTime (can be null)
DateTime? optionalTime = DateTime.UtcNow;
ModOptions.ForName.SaveConfig("optionalEvent", optionalTime);

// Save null
DateTime? nullTime = null;
ModOptions.ForName.SaveConfig("optionalEvent", nullTime);  // Stores as 0

// Load nullable DateTime
var loaded = ModOptions.ForName.LoadConfig<DateTime?>("optionalEvent");
// Returns null if the stored value is 0
```

## Implementation Details

### Storage Format

All DateTime types are stored as **Unix seconds** (long integer):
- Format: Seconds since `1970-01-01 00:00:00 UTC`
- Type: `long` (Int64)
- Example: `1704067200` = `2024-01-01 00:00:00 UTC`

### Null Handling

For nullable types (`DateTime?` and `DateTimeOffset?`):
- `null` → stored as `0`
- `0` → loaded as `null`
- This convention allows clear distinction between "not set" (null) and actual Unix epoch (unlikely to be used)

### UTC Conversion

When saving `DateTime` values:
- `DateTimeKind.Utc` → used as-is
- `DateTimeKind.Local` → automatically converted to UTC
- `DateTimeKind.Unspecified` → treated as UTC

When loading `DateTime` values:
- Always returned with `DateTimeKind.Utc`

### DateTimeOffset Behavior

`DateTimeOffset` inherently includes timezone information:
- Uses `DateTimeOffset.ToUnixTimeSeconds()` when saving
- Uses `DateTimeOffset.FromUnixTimeSeconds()` when loading
- Result is always in UTC (offset = +00:00)

## Error Handling

### Loading Errors

If the stored value is not in Unix timestamp format (not a `long`):
```csharp
var result = ModOptions.ForName.LoadConfig<DateTime>("key", defaultValue);
// Logs error: "[ModOptions] Failed to load DateTime type for key 'key'. Expected Unix seconds (long) format."
// Returns: defaultValue
```

### Type Mismatches

Attempting to convert non-DateTime types will throw `ArgumentException`:
```csharp
// This is handled internally, users won't encounter this
ConvertToUnixSeconds("string");  // ArgumentException
```

## Examples

### Tracking Last Played Time

```csharp
public class SaveManager
{
    private const string LastPlayedKey = "LastPlayedTime";
    
    public void SaveGameState()
    {
        var now = DateTime.UtcNow;
        ModOptions.ForName.SaveConfig(LastPlayedKey, now);
    }
    
    public TimeSpan GetTimeSinceLastPlayed()
    {
        var lastPlayed = ModOptions.ForName.LoadConfig<DateTime>(
            LastPlayedKey, 
            DateTime.UtcNow
        );
        return DateTime.UtcNow - lastPlayed;
    }
}
```

### Optional Event Timestamp

```csharp
public class EventTracker
{
    private const string EventKey = "SpecialEventTime";
    
    public void SetEventTime(DateTime? eventTime)
    {
        ModOptions.ForName.SaveConfig(EventKey, eventTime);
    }
    
    public bool HasEventOccurred()
    {
        var eventTime = ModOptions.ForName.LoadConfig<DateTime?>(EventKey);
        return eventTime.HasValue;
    }
    
    public DateTime? GetEventTime()
    {
        return ModOptions.ForName.LoadConfig<DateTime?>(EventKey);
    }
}
```

### Scheduling with DateTimeOffset

```csharp
public class Scheduler
{
    private const string ScheduledKey = "NextScheduledAction";
    
    public void ScheduleAction(DateTimeOffset when)
    {
        ModOptions.ForName.SaveConfig(ScheduledKey, when);
    }
    
    public bool IsActionDue()
    {
        var scheduled = ModOptions.ForName.LoadConfig<DateTimeOffset>(
            ScheduledKey,
            DateTimeOffset.MaxValue
        );
        return DateTimeOffset.UtcNow >= scheduled;
    }
}
```

## Testing

Comprehensive tests are available in `ModOptionsDateTimeTests.cs`:
- Type detection for all DateTime variants
- Null handling and conversion
- Round-trip conversions
- Edge cases (Unix epoch, negative timestamps, far future dates)
- Error handling

## Migration from Previous Versions

If you have existing DateTime values stored in a different format:

```csharp
// Old format (if you were using ES3's default serialization)
// You'll need to manually migrate or delete old keys

public void MigrateOldDateTimeFormat(string key)
{
    try
    {
        // Try to load as old format (this depends on your previous implementation)
        // Then save in new format
        var oldDateTime = /* load using old method */;
        ModOptions.ForName.SaveConfig(key, oldDateTime);
    }
    catch
    {
        // If migration fails, you may need to reset to default
        ModOptions.ForName.SaveConfig(key, DateTime.UtcNow);
    }
}
```

## Best Practices

1. **Always use UTC times** when possible to avoid timezone confusion
2. **Use nullable types** when a timestamp is optional
3. **Store default values** when creating new keys to ensure consistency
4. **Handle load errors** gracefully with appropriate defaults
5. **Document your timestamp semantics** (e.g., "last played", "created at")

## Technical Notes

- Unix timestamp range: `-9,223,372,036,854,775,808` to `9,223,372,036,854,775,807` seconds
- DateTime range: Approx. `0001-01-01` to `9999-12-31`
- DateTimeOffset uses built-in .NET conversion methods for accuracy
- All conversions preserve second precision (fractional seconds are truncated)
