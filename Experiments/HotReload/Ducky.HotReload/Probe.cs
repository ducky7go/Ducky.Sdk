using System;
using System.Runtime.CompilerServices;

namespace Ducky.HotReload
{
    /// <summary>
    /// State that lives in the shim and therefore survives every Core reload. Core patches/subscribes to it,
    /// the shim checks afterwards that a reload left nothing behind.
    /// </summary>
    public static class Probe
    {
        public static int Hits;
        public static string ActiveMarker = "none";
        public static event Action? Tick;

        public static int TickHandlerCount => Tick?.GetInvocationList().Length ?? 0;

        public static void RaiseTick() => Tick?.Invoke();

        /// <summary>Patched by Core with a postfix that increments <see cref="Hits"/>.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Ping() => 0;

        /// <summary>Number of postfix hits caused by one Ping(): 1 = exactly one live patch, 0 = none, >1 = leaked patches.</summary>
        public static int PingHits()
        {
            var before = Hits;
            Ping();
            return Hits - before;
        }
    }
}
