#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Ducky.Sdk.HotReload
{
    /// <summary>
    /// The decisions of the hot reload shim that do not need Unity: which Core is the newest, when a Core file has been
    /// stable long enough to load, and when a same-name Core must be refused. Plain C#, compiled into the shim
    /// (generated from HotReload/Shim.cs.template) and into the SDK's own tests, so the rules are tested directly.
    /// </summary>
    internal static class HotReloadSchedule
    {
        /// <summary>What the shim does after looking at the newest Core file on one poll.</summary>
        internal enum Step
        {
            /// <summary>Nothing to do: the running Core is current, or this file was already handled.</summary>
            None,

            /// <summary>A newer Core is there but still being written; look again on the next poll.</summary>
            Wait,

            /// <summary>An assembly name that is already loaded in this session: refused, the old code keeps running.</summary>
            Reject,

            /// <summary>A newer Core that has been stable for the whole window: reload now.</summary>
            Reload,
        }

        /// <summary>The newest Core by stamp. Stamps are fixed-width UTC timestamps, so the file name order is the build order.</summary>
        internal static string? Newest(IEnumerable<(string Name, DateTime Write)> cores) =>
            cores
                .OrderByDescending(c => c.Name, StringComparer.Ordinal)
                .ThenByDescending(c => c.Write)
                .Select(c => c.Name)
                .FirstOrDefault();

        /// <summary>
        /// Tracks the newest Core file across polls. A file must keep the same write time for <see cref="StableSeconds"/>
        /// before it is loaded, so a copy that is still in progress is not loaded.
        /// </summary>
        internal sealed class CoreWatch
        {
            private string? _candidateName;
            private DateTime _candidateWrite;
            private double _candidateSince;
            private DateTime _rejectedWrite = DateTime.MinValue;

            internal CoreWatch(double stableSeconds)
            {
                StableSeconds = stableSeconds;
            }

            internal double StableSeconds { get; }

            /// <param name="name">Assembly name (file name without .dll) of the newest Core file.</param>
            /// <param name="write">Its last write time (UTC).</param>
            /// <param name="now">Current time in seconds (monotonic).</param>
            /// <param name="loadedName">Assembly name of the running Core, or null before the first load.</param>
            /// <param name="loadedWrite">Write time the running Core was loaded with.</param>
            /// <param name="loadedNames">Every assembly name loaded in this session.</param>
            internal Step Observe(string name, DateTime write, double now, string? loadedName, DateTime loadedWrite,
                IReadOnlyCollection<string> loadedNames)
            {
                if (name == loadedName && write == loadedWrite)
                {
                    _candidateName = null;
                    return Step.None;
                }

                // The same assembly name again: the game keeps the first assembly with that name, so the old code would run.
                if (loadedNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    _candidateName = null;
                    if (write == _rejectedWrite)
                    {
                        return Step.None;
                    }

                    _rejectedWrite = write;
                    return Step.Reject;
                }

                if (name != _candidateName || write != _candidateWrite)
                {
                    _candidateName = name;
                    _candidateWrite = write;
                    _candidateSince = now;
                    return Step.Wait;
                }

                if (now - _candidateSince < StableSeconds)
                {
                    return Step.Wait;
                }

                _candidateName = null;
                return Step.Reload;
            }
        }
    }
}
