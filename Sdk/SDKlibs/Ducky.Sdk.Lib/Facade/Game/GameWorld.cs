using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Duckov.Buffs;
using UnityEngine;

namespace Ducky.Sdk.Facade;

/// <summary>
/// The production <see cref="IGameWorld{TCharacter}"/>: every member is a straight pass-through of the one static call written on
/// it in the interface, with the same arguments, timing and exceptions. It holds no state. Do not add logic here; a mod can check each member against its original call in the real
/// game.
/// </summary>
public sealed class GameWorld : IGameWorld<CharacterMainControl>
{
    /// <summary>The one production instance (the class has no state).</summary>
    public static readonly GameWorld Instance = new();

    private GameWorld()
    {
    }

    public CharacterMainControl? Player => CharacterMainControl.Main;

    // LevelManager.Instance, as null when the game has destroyed it (callers compare the level with a plain null)
    public object? Level => LevelManager.Instance != null ? LevelManager.Instance : null;

    public bool IsBaseLevel => LevelManager.Instance != null && LevelManager.Instance.IsBaseLevel;

    public bool LevelInited => LevelManager.LevelInited;

    public float Now => Time.time;

    public float UnscaledNow => Time.unscaledTime;

    public float DeltaTime => Time.deltaTime;

    public float RandomValue => UnityEngine.Random.value;

    public int RandomRange(int minInclusive, int maxExclusive) => UnityEngine.Random.Range(minInclusive, maxExclusive);

    public (float X, float Y) RandomInsideUnitCircle
    {
        get
        {
            var point = UnityEngine.Random.insideUnitCircle;
            return (point.x, point.y);
        }
    }

    public IReadOnlyList<CharacterMainControl> Enemies()
    {
        var enemies = new List<CharacterMainControl>();
        foreach (var c in UnityEngine.Object.FindObjectsByType<CharacterMainControl>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c != null && !c.IsMainCharacter && c.Team != Teams.player)
            {
                enemies.Add(c);
            }
        }

        return enemies;
    }

    public T[] FindAll<T>()
        where T : class =>
        Typed<T>(UnityEngine.Object.FindObjectsByType(typeof(T), FindObjectsInactive.Exclude, FindObjectsSortMode.None));

    public T[] FindAllOfType<T>()
        where T : class =>
        Typed<T>(UnityEngine.Object.FindObjectsOfType(typeof(T)));

    public T? FindFirstOfType<T>()
        where T : class =>
        UnityEngine.Object.FindObjectOfType(typeof(T)) as T;

    public void DelayFrames(int frames, Action then) => UniTask.DelayFrame(frames).GetAwaiter().OnCompleted(then);

    public void DelaySeconds(float seconds, Action then) => UniTask.Delay(TimeSpan.FromSeconds(seconds)).GetAwaiter().OnCompleted(then);

    public void Explode(WorldPoint center, float radius, ExplosionRequest<CharacterMainControl> request)
    {
        // the receiver first, as the original call evaluates it (LevelManager.Instance.ExplosionManager), then the damage info
        var manager = LevelManager.Instance.ExplosionManager;
        var damage = new DamageInfo(request.Source!)
        {
            damageValue = request.Damage,
            isExplosion = request.IsExplosion,
            damageType = request.RealDamage ? DamageTypes.realDamage : DamageTypes.normal,
            isFromBuffOrEffect = request.FromBuffOrEffect,
            ignoreDifficulty = request.IgnoreDifficulty,
            buff = (Buff?)request.Buff,
            buffChance = request.BuffChance,
        };
        var fx = request.Fx switch
        {
            ExplosionFx.Normal => ExplosionFxTypes.normal,
            ExplosionFx.Flash => ExplosionFxTypes.flash,
            ExplosionFx.Custom => ExplosionFxTypes.custom,
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Fx, null),
        };
        manager.CreateExplosion(ToVector(center), radius, damage, fx, request.ShakeStrength, request.CanHurtSelf);
    }

    /// <summary>A game position as the facade's point: the same three floats.</summary>
    public static WorldPoint ToWorld(Vector3 position) => new(position.x, position.y, position.z);

    /// <summary>The facade's point as a game position: the same three floats.</summary>
    public static Vector3 ToVector(WorldPoint point) => new(point.X, point.Y, point.Z);

    // the generic Object.FindObjects*<T> casts each found object to T like this; T may be an interface, which the generic form cannot take
    private static T[] Typed<T>(UnityEngine.Object[] found)
        where T : class
    {
        var typed = new T[found.Length];
        for (var i = 0; i < found.Length; i++)
        {
            typed[i] = (found[i] as T)!;
        }

        return typed;
    }
}
