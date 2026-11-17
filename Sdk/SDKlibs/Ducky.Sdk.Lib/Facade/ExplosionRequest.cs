namespace Ducky.Sdk.Facade;

/// <summary>The look of an explosion: the game's <c>ExplosionFxTypes</c> values a mod feature uses (<c>GameWorld</c> maps them one to one).</summary>
public enum ExplosionFx
{
    /// <summary>The game's normal explosion (<c>ExplosionFxTypes.normal</c>).</summary>
    Normal,

    /// <summary>The screen-covering flash (<c>ExplosionFxTypes.flash</c>).</summary>
    Flash,

    /// <summary>The caller's own look (<c>ExplosionFxTypes.custom</c>); the game shows nothing for it, so the caller draws the effect.</summary>
    Custom,
}

/// <summary>
/// What one explosion is asked for, apart from its centre and radius (those are the arguments of <see cref="IGameWorld{TCharacter}.Explode"/>).
/// Each field is the game's field that it names: the damage info fields of <c>DamageInfo</c> and the arguments of
/// <c>ExplosionManager.CreateExplosion</c>. A caller sets the fields its explosion needs; the production class sets every field on the game's
/// <c>DamageInfo</c>, and a field left at its default here is the value the game's <c>DamageInfo</c> constructor gives it.
/// </summary>
/// <typeparam name="TCharacter">The game's character type (the source of the damage).</typeparam>
public sealed class ExplosionRequest<TCharacter>
    where TCharacter : class
{
    /// <summary>The character the explosion comes from (<c>DamageInfo.fromCharacter</c>), or null when there is none.</summary>
    public TCharacter? Source { get; init; }

    /// <summary>The damage (<c>damageValue</c>).</summary>
    public float Damage { get; init; }

    /// <summary><c>isExplosion</c>.</summary>
    public bool IsExplosion { get; init; }

    /// <summary>True for <c>DamageTypes.realDamage</c>; false for <c>DamageTypes.normal</c>.</summary>
    public bool RealDamage { get; init; }

    /// <summary><c>isFromBuffOrEffect</c>.</summary>
    public bool FromBuffOrEffect { get; init; }

    /// <summary><c>ignoreDifficulty</c>.</summary>
    public bool IgnoreDifficulty { get; init; }

    /// <summary>
    /// The buff the damage may apply (<c>buff</c>). It is the game's <c>Buff</c> asset, which a pure caller does not name, so it travels as an object and
    /// the production class casts it back.
    /// </summary>
    public object? Buff { get; init; }

    /// <summary><c>buffChance</c>: the chance from 0 to 1 that <see cref="Buff"/> applies.</summary>
    public float BuffChance { get; init; }

    /// <summary>The look of the explosion (<c>fxType</c>).</summary>
    public ExplosionFx Fx { get; init; } = ExplosionFx.Normal;

    /// <summary>The camera shake (<c>shakeStrength</c>). 1 is the game's default.</summary>
    public float ShakeStrength { get; init; } = 1f;

    /// <summary>Whether the explosion hurts the side of its source (<c>canHurtSelf</c>).</summary>
    public bool CanHurtSelf { get; init; }

    /// <summary>
    /// The explosion most features make: a real explosion of <paramref name="damage"/> from <paramref name="source"/> that comes from a
    /// buff or effect and ignores the level's difficulty, with the normal look and the game's default shake.
    /// </summary>
    /// <param name="source">The character the explosion comes from.</param>
    /// <param name="damage">The damage.</param>
    /// <param name="canHurtSelf">Whether the explosion hurts the side of <paramref name="source"/>.</param>
    public static ExplosionRequest<TCharacter> Blast(TCharacter? source, float damage, bool canHurtSelf) => new()
    {
        Source = source,
        Damage = damage,
        IsExplosion = true,
        RealDamage = true,
        FromBuffOrEffect = true,
        IgnoreDifficulty = true,
        Fx = ExplosionFx.Normal,
        ShakeStrength = 1f,
        CanHurtSelf = canHurtSelf,
    };
}
