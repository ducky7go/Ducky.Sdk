using System;
using Ducky.Sdk.Utils;

namespace Ducky.Sdk.Restore;

/// <summary>
/// A nesting counter that marks restorations made by the SDK's own helpers, so that rules can tell them from item use.
/// <see cref="CharacterRestore"/> enters a scope around the game call; the scope is released when its handle is disposed,
/// including when the call throws. Main thread only, as the game's restoration calls are.
/// </summary>
internal static class RestoreOriginScope
{
    private static int _depth;

    /// <summary>True while at least one scope is open, meaning the restoration being made comes from a mod.</summary>
    public static bool IsActive => _depth > 0;

    /// <summary>Opens a scope. Dispose the returned handle to close it; disposing it again has no further effect.</summary>
    public static IDisposable Enter()
    {
        _depth++;
        return new DisposeOnce(() => _depth--);
    }
}
