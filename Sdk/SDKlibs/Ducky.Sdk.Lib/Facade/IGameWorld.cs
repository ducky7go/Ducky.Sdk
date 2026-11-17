using System;
using System.Collections.Generic;

namespace Ducky.Sdk.Facade;

/// <summary>
/// The game as a mod feature sees it: the player, the level, game time, random numbers, the objects in the scene and
/// delayed calls. The contract has no game types, so the game's character is the type argument <typeparamref name="TCharacter"/>
/// (the game's <c>CharacterMainControl</c> in production, a plain object in a test).
/// <para>
/// Every member is a straight pass-through of ONE original static call; the production class is <c>GameWorld</c>
/// of the SDK. The call each member stands for is written on the member, so a mod can check each one against that call in the real
/// game. Do not add a member that does more than its original call, and do not change what a member reads or when it reads it.
/// </para>
/// </summary>
/// <typeparam name="TCharacter">The game's character type (<c>CharacterMainControl</c>).</typeparam>
public interface IGameWorld<TCharacter>
    where TCharacter : class
{
    /// <summary>The player's character, or null. Wraps <c>CharacterMainControl.Main</c> (the game returns <c>LevelManager.Instance.MainCharacter</c>, or null with no level manager).</summary>
    TCharacter? Player { get; }

    /// <summary>
    /// The current level object, or null: the identity of the level, which changes when a level is rebuilt (compare it with
    /// <see cref="object.ReferenceEquals"/>). Wraps <c>LevelManager.Instance</c>; the Unity null check is done by the production class.
    /// </summary>
    object? Level { get; }

    /// <summary>True when the current level is the base. Wraps <c>LevelManager.Instance.IsBaseLevel</c>; false when there is no level manager (<see cref="Level"/> is null).</summary>
    bool IsBaseLevel { get; }

    /// <summary>True when the level is set up. Wraps <c>LevelManager.LevelInited</c>.</summary>
    bool LevelInited { get; }

    /// <summary>Game time in seconds, scaled. Wraps <c>Time.time</c>.</summary>
    float Now { get; }

    /// <summary>Real time in seconds, not scaled. Wraps <c>Time.unscaledTime</c>.</summary>
    float UnscaledNow { get; }

    /// <summary>The seconds the last frame took, scaled. Wraps <c>Time.deltaTime</c>.</summary>
    float DeltaTime { get; }

    /// <summary>A random number in [0, 1]. Wraps <c>UnityEngine.Random.value</c>.</summary>
    float RandomValue { get; }

    /// <summary>A random integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>). Wraps <c>UnityEngine.Random.Range(int, int)</c>.</summary>
    int RandomRange(int minInclusive, int maxExclusive);

    /// <summary>A random point in the unit circle. Wraps <c>UnityEngine.Random.insideUnitCircle</c> (x, y).</summary>
    (float X, float Y) RandomInsideUnitCircle { get; }

    /// <summary>
    /// The characters of the scene that are not the player and not on the player's team. Wraps
    /// <c>Object.FindObjectsByType&lt;CharacterMainControl&gt;(FindObjectsInactive.Exclude, FindObjectsSortMode.None)</c>
    /// filtered by <c>c != null &amp;&amp; !c.IsMainCharacter &amp;&amp; c.Team != Teams.player</c> (the filter of the enemy spawn hook).
    /// A caller whose own filter differs (for example it also needs the character alive) uses <see cref="FindAll{T}"/> instead.
    /// </summary>
    IReadOnlyList<TCharacter> Enemies();

    /// <summary>All active objects of type <typeparamref name="T"/> in the scene, as the array the game returns. Wraps <c>Object.FindObjectsByType&lt;T&gt;(FindObjectsInactive.Exclude, FindObjectsSortMode.None)</c>.</summary>
    T[] FindAll<T>() where T : class;

    /// <summary>All active objects of type <typeparamref name="T"/>, found the old way. Wraps <c>Object.FindObjectsOfType&lt;T&gt;()</c>.</summary>
    T[] FindAllOfType<T>() where T : class;

    /// <summary>The first active object of type <typeparamref name="T"/>, or null. Wraps <c>Object.FindObjectOfType&lt;T&gt;()</c>.</summary>
    T? FindFirstOfType<T>() where T : class;

    /// <summary>Calls <paramref name="then"/> after <paramref name="frames"/> frames. Wraps <c>UniTask.DelayFrame(frames).GetAwaiter().OnCompleted(then)</c>.</summary>
    void DelayFrames(int frames, Action then);

    /// <summary>Calls <paramref name="then"/> after <paramref name="seconds"/> seconds of game time. Wraps <c>UniTask.Delay(TimeSpan.FromSeconds(seconds)).GetAwaiter().OnCompleted(then)</c>.</summary>
    void DelaySeconds(float seconds, Action then);

    /// <summary>
    /// An explosion at <paramref name="center"/> with <paramref name="radius"/>, as <paramref name="request"/> asks for it. Wraps
    /// <c>LevelManager.Instance.ExplosionManager.CreateExplosion(center, radius, damageInfo, fx, shakeStrength, canHurtSelf)</c>, where
    /// <c>damageInfo</c> is <c>new DamageInfo(source)</c> with the request's fields set. The production class does no null check, so with no
    /// level manager it fails in the same way the original call did.
    /// </summary>
    void Explode(WorldPoint center, float radius, ExplosionRequest<TCharacter> request);
}
