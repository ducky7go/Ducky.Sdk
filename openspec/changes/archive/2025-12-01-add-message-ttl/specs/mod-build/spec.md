## ADDED Requirements

### Requirement: Message TTL Support
The ModHttpV1 message system SHALL support Time-To-Live (TTL) for messages to prevent indefinite queuing and resource waste.

#### Scenario: Message expires before processing
- **WHEN** a message has been in queue longer than its TTL (default 60 seconds)
- **THEN** the message SHALL be discarded during processing
- **AND** a debug log entry SHALL be created indicating message expiration

#### Scenario: Message processed within TTL
- **WHEN** a message is processed before its TTL expires
- **THEN** the message SHALL be delivered normally to the registered handler
- **AND** no TTL-related logging SHALL occur

#### Scenario: Custom TTL configuration
- **WHEN** the system needs different TTL values
- **THEN** the TTL value SHALL be configurable via system settings
- **AND** the default TTL SHALL remain 60 seconds if not configured

## MODIFIED Requirements

### Requirement: Message Queue Processing
The ModHttpV1 system SHALL process messages from the queue while validating their TTL before delivery.

#### Scenario: Processing messages with TTL check
- **WHEN** processing messages from the queue
- **THEN** the system SHALL check if message timestamp + TTL < current time
- **AND** expired messages SHALL be skipped and removed from queue
- **AND** valid messages SHALL be delivered to the registered callback
- **AND** processing SHALL continue with next message if current is expired

#### Scenario: Queue management with expired messages
- **WHEN** the queue contains expired messages
- **THEN** expired messages SHALL be removed during normal processing
- **AND** the queue size SHALL reflect only non-expired messages
- **AND** periodic cleanup SHALL run every 30 seconds to remove expired messages

#### Scenario: Expired messages cleanup
- **WHEN** CleanupExpiredMessagesAsync runs
- **THEN** all expired messages SHALL be removed regardless of handler registration
- **AND** valid messages SHALL be preserved in their original order
- **AND** cleanup statistics SHALL be logged when expired messages are found