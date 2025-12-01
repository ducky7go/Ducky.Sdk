# Change: Add configurable logging control for ModHttpV1

## Why
ModHttpV1 and ModHttpV1Proxy are high-frequency communication components that generate extensive log output during normal operation. This verbose logging can:
- Obscure important application logs
- Impact performance due to frequent I/O operations
- Make debugging difficult by flooding the console with routine messages
- Consume unnecessary disk space when logging to files

## What Changes
- **ADDED**: ModOptions configuration for controlling ModHttpV1 protocol logging with simple on/off switch
- **MODIFIED**: ModHttpV1 to respect logging configuration settings
- **MODIFIED**: ModHttpV1Proxy to respect logging configuration settings
- **ADDED**: Proper encapsulation with read-only configuration properties
- **DEFAULT**: Logging disabled by default to reduce noise

## Impact
- Affected specs: mod-options (configuration management), mod-build (mod communication protocols)
- Affected code: ModHttpV1.cs, ModHttpV1Proxy.cs, ModOptions system
- Backward compatibility: Maintained - existing behavior with logging enabled can be restored via configuration
- Performance: Improved when logging is disabled (default state)