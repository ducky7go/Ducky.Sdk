## 1. Core Implementation
- [x] 1.1 Create ModHttpV1Host.cs in Contracts/ModProtocols/
  - [x] Move ModHttpV1 implementation from MessageHubHost sample
  - [x] Rename class to ModHttpV1Host for clarity
  - [x] Update namespace to Ducky.Sdk.Contracts.ModProtocols
  - [x] Ensure proper cleanup and lifecycle management
- [x] 1.2 Update ModBehaviourBase.cs
  - [x] Add EnableMessageHubHost property (default: true)
  - [x] Add IsMessageHubHost read-only property
  - [x] Add MessageHub host detection logic in ModEnabled()
  - [x] Add host startup logic when no existing host found
  - [x] Add host registration and client connection logic
- [x] 1.3 Create MessageHubManager utility class
  - [x] Implement singleton pattern for host detection
  - [x] Add methods for host registration and discovery
  - [x] Keep host running permanently once started

## 2. Integration and Compatibility
- [x] 2.1 Update ModHttpV1Proxy to work with new host
  - [x] Ensure proxy can find and connect to SDK-integrated host
  - [x] Maintain backward compatibility with existing client code
- [x] 2.2 Update build system
  - [x] Include new ModHttpV1.cs in SDK compilation
  - [x] Ensure proper dependencies are referenced
- [x] 2.3 Update sample projects
  - [x] Update documentation to reflect new integrated host approach

## 3. Documentation
- [x] 3.1 Update SDK documentation
  - [x] Document MessageHub auto-hosting feature
  - [x] Document EnableMessageHubHost property usage
  - [x] Add migration guide from MessageHubHost mod
- [x] 3.2 Update README
  - [x] Remove references to separate MessageHubHost mod
  - [x] Add section about integrated MessageHub functionality
- [x] 3.3 Create example code snippets
  - [x] Show how to disable host functionality
  - [x] Show how to check if mod is host
  - [x] Show inter-mod communication examples