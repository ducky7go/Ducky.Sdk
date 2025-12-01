# Change: Add TTL support for ModHttpV1 messages

## Why
Currently, messages in ModHttpV1 queue can accumulate indefinitely if the target mod is not registered or has been unregistered. This leads to memory leaks and potential performance degradation over time. Adding TTL (Time To Live) ensures that expired messages are automatically discarded, improving system reliability and resource management.

## What Changes
- **ADDED**: TTL field to MessageItem structure with configurable default value (1 minute)
- **ADDED**: Background cleanup task (CleanupExpiredMessagesAsync) to remove expired messages from queues
- **MODIFIED**: Notify method to include TTL timestamp when creating messages
- **MODIFIED**: ProcessMessageQueueAsync to check TTL before processing messages
- **ADDED**: Configuration option to set default TTL value (60 seconds)

## Impact
- Affected specs: mod-build (mod communication protocols)
- Affected code: ModHttpV1.cs, MessageItem record, message processing logic
- Backward compatibility: Maintained - existing API unchanged, only new optional behavior added