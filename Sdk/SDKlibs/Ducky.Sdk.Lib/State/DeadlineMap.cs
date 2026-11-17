using System;
using System.Collections.Generic;

namespace Ducky.Sdk.State;

/// <summary>
/// The keys (enemies) a feature has put into a timed state, each with the clock time the state ends (or the time before which it
/// must not start again). The feature's update asks <see cref="Expired"/> which ones are over and then does what it does at the
/// end of the state.
/// <para>
/// A key is also over when the game object behind it is gone. The game's "destroyed" check (<c>character == null</c> on a Unity
/// object) cannot be made here, so the caller passes it in as <c>isGone</c>.
/// </para>
/// </summary>
/// <typeparam name="TKey">What the feature remembers (the game's character).</typeparam>
public sealed class DeadlineMap<TKey>
    where TKey : notnull
{
    private readonly AppliedSet<TKey, float> _until = new();

    /// <summary>How many keys are in the state.</summary>
    public int Count => _until.Count;

    /// <summary>The keys. The map must not change while they are enumerated.</summary>
    public Dictionary<TKey, float>.KeyCollection Keys => _until.Keys;

    /// <summary>Whether <paramref name="key"/> is in the state.</summary>
    public bool Contains(TKey key) => _until.ContainsKey(key);

    /// <summary>Puts <paramref name="key"/> in the state until <paramref name="until"/> (replacing an earlier end time).</summary>
    public void Set(TKey key, float until) => _until[key] = until;

    /// <summary>Reads the end time of <paramref name="key"/>; false when it is not in the state.</summary>
    public bool TryGet(TKey key, out float until) => _until.TryGetValue(key, out until);

    /// <summary>Takes <paramref name="key"/> out of the state. Returns false when it was not in it.</summary>
    public bool Remove(TKey key) => _until.Remove(key);

    /// <summary>Takes every key out of the state.</summary>
    public void Clear() => _until.Clear();

    /// <summary>
    /// The keys that are over at <paramref name="now"/>, in the order the map holds them: those whose end time has been reached
    /// (<c>now &gt;= until</c>, so a state ends exactly at its end time) and those <paramref name="isGone"/> reports. The map is not
    /// changed; the caller removes each key (and does what the end of the state needs).
    /// </summary>
    public List<TKey> Expired(float now, Func<TKey, bool> isGone)
    {
        var done = new List<TKey>();
        foreach (var pair in _until)
        {
            if (now >= pair.Value || isGone(pair.Key))
            {
                done.Add(pair.Key);
            }
        }

        return done;
    }
}
