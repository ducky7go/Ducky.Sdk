## ADDED Requirements

### Requirement: MessageHub Host Auto-Startup
The SDK SHALL automatically start a MessageHub host when the first mod with host functionality enabled initializes.

#### Scenario: First mod starts MessageHub host
- **WHEN** a mod loads and `EnableMessageHubHost` is true (default)
- **AND** no other MessageHub host is currently running
- **THEN** the mod becomes the active MessageHub host
- **AND** other mods can connect to it for inter-mod communication

#### Scenario: Subsequent mods detect existing host
- **WHEN** a mod loads and `EnableMessageHubHost` is true
- **AND** another MessageHub host is already running
- **THEN** the mod connects to the existing host as a client
- **AND** does not start a new host instance

### Requirement: MessageHub Host Configuration
ModBehaviourBase SHALL provide configuration to control MessageHub host behavior.

#### Scenario: Mod disables MessageHub host functionality
- **WHEN** a mod sets `EnableMessageHubHost` to false
- **THEN** the mod does not attempt to start or become a MessageHub host
- **AND** can still connect to other mods' MessageHub hosts

#### Scenario: Mod checks host status
- **WHEN** a mod needs to know if it's the MessageHub host
- **THEN** `IsMessageHubHost` property returns true if the mod is the active host
- **AND** returns false if it's a client or host functionality is disabled

### Requirement: MessageHub Backward Compatibility
Existing mods SHALL continue to work without any code changes.

#### Scenario: Existing mod without MessageHub configuration
- **WHEN** an existing mod loads without any MessageHub configuration
- **THEN** the mod works exactly as before
- **AND** automatically gets MessageHub host functionality (can be opted out)

#### Scenario: Existing MessageHubHost mod users
- **WHEN** users currently using Ducky.MessageHubHost mod
- **THEN** they can remove the separate MessageHubHost mod
- **AND** any other mod can become the host automatically

### Requirement: Host Lifecycle Management
The MessageHub host SHALL remain running permanently once started.

#### Scenario: Host remains running after mod disables
- **WHEN** the active MessageHub host mod is disabled
- **THEN** the host continues running indefinitely
- **AND** other mods can continue using it
- **AND** no shutdown or cleanup is performed