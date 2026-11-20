using System;
using System.Collections.Generic;
using System.Linq;
using Ducky.Sdk.Logging;

namespace Ducky.Sdk.Utils;

/// <summary>
/// The patches of this copy of the SDK (and of the mod that carries it) that could not be installed. A game update that
/// renames a patched method makes the install fail with one log line; without this record the mod that registered a rule
/// or a hook cannot tell that nothing will ever run. Read <see cref="Failures"/> after start-up and show it where the
/// player or a test can see it. Touches no Unity object.
/// </summary>
/// <remarks>
/// Every mod has its own copy, so this lists only the patches of the mod that reads it. An entry is removed when the same
/// patch is installed successfully later; an entry whose key is a message text (see <see cref="Fail"/>) stays until the
/// game restarts.
/// </remarks>
public static class PatchHealth
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, string> Failed = new();

    /// <summary>One patch that could not be installed.</summary>
    public readonly struct Failure
    {
        /// <summary>Creates a failure.</summary>
        public Failure(string key, string message)
        {
            Key = key;
            Message = message;
        }

        /// <summary>The patch id, or the message text when the caller has no id.</summary>
        public string Key { get; }

        /// <summary>The log message of the failure, with the exception text.</summary>
        public string Message { get; }
    }

    /// <summary>True when at least one patch of this copy could not be installed.</summary>
    public static bool HasFailures
    {
        get
        {
            lock (Sync)
            {
                return Failed.Count > 0;
            }
        }
    }

    /// <summary>The patches that could not be installed, in no particular order.</summary>
    public static IReadOnlyList<Failure> Failures
    {
        get
        {
            lock (Sync)
            {
                return Failed.Select(pair => new Failure(pair.Key, pair.Value)).ToArray();
            }
        }
    }

    /// <summary>True when the patch with <paramref name="key"/> is recorded as failed.</summary>
    public static bool IsFailed(string key)
    {
        lock (Sync)
        {
            return Failed.ContainsKey(key);
        }
    }

    /// <summary>
    /// Logs <paramref name="failure"/> as an error with <paramref name="ex"/> and records it under <paramref name="key"/>
    /// (the patch id, or the message text when there is no id).
    /// </summary>
    public static void Fail(string key, Exception ex, string failure)
    {
        Log.Error(ex, failure);
        lock (Sync)
        {
            Failed[key] = failure + ": " + ex.GetType().Name + ": " + ex.Message;
        }
    }

    /// <summary>Same as <see cref="Fail(string, Exception, string)"/>, with the message text as the key.</summary>
    public static void Fail(Exception ex, string failure) => Fail(failure, ex, failure);

    /// <summary>Removes the record of <paramref name="key"/>, after the patch was installed successfully.</summary>
    public static void Succeeded(string key)
    {
        lock (Sync)
        {
            Failed.Remove(key);
        }
    }

    /// <summary>Forgets every record. For tests.</summary>
    internal static void Reset()
    {
        lock (Sync)
        {
            Failed.Clear();
        }
    }
}
