# Change: Integrate MessageHub Host into SDK Core

## Why
Currently, mods that want to use MessageHub functionality require a separate MessageHubHost mod to be installed. This creates an unnecessary dependency and deployment complexity. By integrating the MessageHub host functionality directly into the SDK core library, every mod can act as a potential host, eliminating the need for a separate host mod and simplifying the mod ecosystem.

## What Changes
- Move ModHttpV1 implementation from Ducky.MessageHubHost sample to SDK core library under `Contracts/ModProtocols/`
- Add automatic MessageHub host detection and startup in `ModBehaviourBase.ModEnabled()`
- Add `EnableMessageHubHost` property to `ModBehaviourBase` to allow mods to opt out of host functionality
- Ensure only the first mod to start the host becomes the active host
- Maintain backward compatibility with existing ModHttpV1Proxy and client contracts

## Impact
- **Affected specs**: mod-build (adds new MessageHub host capabilities)
- **Affected code**:
  - `Sdk/SDKlibs/Ducky.Sdk.Lib/Contracts/ModProtocols/` - new ModHttpV1Host.cs
  - `Sdk/SDKlibs/Ducky.Sdk.Lib/ModBehaviours/ModBehaviourBase.cs` - host startup logic
- **Breaking changes**: None - existing mods continue to work unchanged
- **New capabilities**: Any mod can become MessageHub host without requiring separate installation