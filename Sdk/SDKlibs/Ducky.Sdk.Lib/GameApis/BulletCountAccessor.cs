using System;
using System.Reflection;

namespace Ducky.Sdk.GameApis;

/// <summary>
/// Reads and writes the <c>bulletCount</c> member of a gun whether the game declares it as a property or as a field,
/// public or not. Resolved once per type by <see cref="Create"/>. Pure reflection, so it is tested with stand-in types.
/// </summary>
/// <remarks>
/// Member kind (checked in game source, Newbe.Dod/decompiles/TeamSoda.Duckov.Core/ItemSetting_Gun.cs): <c>bulletCount</c> is a
/// <b>private instance property</b> with a getter and a setter, backed by <c>_bulletCountCache</c>; the public
/// <c>BulletCount</c> property is a separate member. Dod's <c>Utils.SetBulletCount</c> reflects the same private property.
/// </remarks>
internal sealed class BulletCountAccessor
{
    internal const string MemberName = "bulletCount";

    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private readonly Func<object, int> _get;
    private readonly Action<object, int>? _set;

    private BulletCountAccessor(Func<object, int> get, Action<object, int>? set)
    {
        _get = get;
        _set = set;
    }

    /// <summary>The accessor for <paramref name="type"/>, or null when it has no <c>bulletCount</c> property or field.</summary>
    internal static BulletCountAccessor? Create(Type type)
    {
        var property = type.GetProperty(MemberName, InstanceMembers);
        if (property != null && property.CanRead)
        {
            return new BulletCountAccessor(
                target => (int)property.GetValue(target, null)!,
                property.CanWrite ? (target, value) => property.SetValue(target, value, null) : null);
        }

        var field = type.GetField(MemberName, InstanceMembers);
        if (field != null)
        {
            return new BulletCountAccessor(
                target => (int)field.GetValue(target)!,
                (target, value) => field.SetValue(target, value));
        }

        return null;
    }

    /// <summary>True when the bullet count can be written. A read-only property returns false.</summary>
    internal bool CanSet => _set != null;

    internal int Get(object target) => _get(target);

    /// <summary>Writes the count. Returns false when the member cannot be written.</summary>
    internal bool TrySet(object target, int value)
    {
        if (_set == null)
        {
            return false;
        }

        _set(target, value);
        return true;
    }
}
