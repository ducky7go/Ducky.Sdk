# Ducky.MessageHubHost Sample

## ⚠️ Important Notice

This sample is now **deprecated**. The MessageHub host functionality has been integrated directly into the Ducky.Sdk core library.

## Migration Guide

Starting from the latest Ducky.Sdk version:
- **No separate MessageHubHost mod is required**
- Any mod with `ModBehaviourBase` automatically gets MessageHub host capabilities
- The first mod to load will automatically become the MessageHub host

### How it works now
```csharp
// Your ModBehaviour automatically gets MessageHub host functionality
public class MyModBehaviour : ModBehaviourBase
{
    // EnableMessageHubHost defaults to true
    // You can disable it if needed:
    protected override bool EnableMessageHubHost { get; set; } = false;

    protected override void ModEnabled()
    {
        // Check if this mod is the host
        if (IsMessageHubHost)
        {
            Log.Info("This mod is the MessageHub host!");
        }

        // Your mod logic here...
    }
}
```

## Benefits
- ✅ No need to install a separate MessageHubHost mod
- ✅ Any mod can be the host
- ✅ Backward compatible with existing client code
- ✅ Automatic host detection and startup

## Existing Code
The implementation in this sample has been moved to:
- `Sdk/SDKlibs/Ducky.Sdk.Lib/Contracts/ModProtocols/ModHttpV1Host.cs`
- `Sdk/SDKlibs/Ducky.Sdk.Lib/Contracts/ModProtocols/MessageHubManager.cs`
- Integrated into `ModBehaviourBase.cs`

You can still reference this code for understanding the MessageHub protocol, but the new SDK-integrated implementation should be used instead.