using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Ducky.Sdk.GameApis;

internal static class FieldExtensions
{
    // caches for compiled setters/getters. Keys include target type, field name and value type where appropriate.
    private static readonly ConcurrentDictionary<string, Delegate> TypedSetters = new();
    private static readonly ConcurrentDictionary<string, Action<object, object>> ObjectSetters = new();

    private static readonly ConcurrentDictionary<string, Delegate> TypedGetters = new();
    private static readonly ConcurrentDictionary<string, Func<object, object>> ObjectGetters = new();

    // --- STATIC FIELD SUPPORT ---
    // Separate caches for static fields (no target instance)
    private static readonly ConcurrentDictionary<string, Delegate> StaticTypedSetters = new();
    private static readonly ConcurrentDictionary<string, Action<object>> StaticObjectSetters = new();

    private static readonly ConcurrentDictionary<string, Delegate> StaticTypedGetters = new();
    private static readonly ConcurrentDictionary<string, Func<object>> StaticObjectGetters = new();

    // Helpers to build cache keys
    private static string TypedKey(Type targetType, string fieldName, Type valueType) => $"{targetType.FullName}:{fieldName}:{valueType.FullName}";
    private static string ObjectKey(Type targetType, string fieldName) => $"{targetType.FullName}:{fieldName}:obj";
    private static string StaticTypedKey(Type targetType, string fieldName, Type valueType) => $"{targetType.FullName}:{fieldName}:{valueType.FullName}:static";
    private static string StaticObjectKey(Type targetType, string fieldName) => $"{targetType.FullName}:{fieldName}:obj:static";

    // --- SETTER CREATORS ---
    private static Action<TTarget, TValue> CreateTypedSetter<TTarget, TValue>(string fieldName)
    {
        var targetType = typeof(TTarget);
        var fieldInfo = targetType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? throw new MissingFieldException(targetType.FullName, fieldName);

        var targetParam = Expression.Parameter(targetType, "target");
        var valueParam = Expression.Parameter(typeof(TValue), "value");

        Expression valueExpr = valueParam;
        if (!fieldInfo.FieldType.IsAssignableFrom(typeof(TValue)))
        {
            valueExpr = Expression.Convert(valueParam, fieldInfo.FieldType);
        }

        var assign = Expression.Assign(Expression.Field(targetParam, fieldInfo), valueExpr);
        return Expression.Lambda<Action<TTarget, TValue>>(assign, targetParam, valueParam).Compile();
    }

    private static Action<TTarget, object> CreateObjectSetter<TTarget>(string fieldName)
    {
        var targetType = typeof(TTarget);
        var fieldInfo = targetType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? throw new MissingFieldException(targetType.FullName, fieldName);

        var targetParam = Expression.Parameter(targetType, "target");
        var valueParam = Expression.Parameter(typeof(object), "value");

        var converted = Expression.Convert(valueParam, fieldInfo.FieldType);
        var assign = Expression.Assign(Expression.Field(targetParam, fieldInfo), converted);
        return Expression.Lambda<Action<TTarget, object>>(assign, targetParam, valueParam).Compile();
    }

    // Reflection-based fallback creators for setters
    private static Action<TTarget, TValue> CreateReflectionTypedSetter<TTarget, TValue>(FieldInfo fieldInfo)
    {
        return (target, value) => fieldInfo.SetValue(target, value);
    }

    private static Action<TTarget, object> CreateReflectionObjectSetter<TTarget>(FieldInfo fieldInfo)
    {
        return (target, value) => fieldInfo.SetValue(target, value);
    }

    // --- GETTER CREATORS ---
    private static Func<TTarget, TValue> CreateTypedGetter<TTarget, TValue>(string fieldName)
    {
        var targetType = typeof(TTarget);
        var fieldInfo = targetType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? throw new MissingFieldException(targetType.FullName, fieldName);

        var targetParam = Expression.Parameter(targetType, "target");
        var fieldExpr = Expression.Field(targetParam, fieldInfo);
        // build the correctly-typed body (convert as needed)
        Expression body = Expression.Convert(fieldExpr, typeof(TValue));

        return Expression.Lambda<Func<TTarget, TValue>>(body, targetParam).Compile();
    }

    private static Func<TTarget, object> CreateObjectGetter<TTarget>(string fieldName)
    {
        var targetType = typeof(TTarget);
        var fieldInfo = targetType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? throw new MissingFieldException(targetType.FullName, fieldName);

        var targetParam = Expression.Parameter(targetType, "target");
        var fieldExpr = Expression.Convert(Expression.Field(targetParam, fieldInfo), typeof(object));
        return Expression.Lambda<Func<TTarget, object>>(fieldExpr, targetParam).Compile();
    }

    // Reflection-based fallback creators for getters
    private static Func<TTarget, TValue> CreateReflectionTypedGetter<TTarget, TValue>(FieldInfo fieldInfo)
    {
        return target => (TValue)fieldInfo.GetValue(target)!;
    }

    private static Func<TTarget, object> CreateReflectionObjectGetter<TTarget>(FieldInfo fieldInfo)
    {
        return target => fieldInfo.GetValue(target)!;
    }

    // --- PUBLIC GET/SET RETRIEVAL (with caching + fallback) ---
    public static Action<TTarget, TValue> GetTypedSetter<TTarget, TValue>(string fieldName)
    {
        var key = TypedKey(typeof(TTarget), fieldName, typeof(TValue));
        if (TypedSetters.TryGetValue(key, out var existing))
        {
            return (Action<TTarget, TValue>)existing!;
        }

        try
        {
            var setter = CreateTypedSetter<TTarget, TValue>(fieldName);
            TypedSetters.TryAdd(key, setter);
            return setter;
        }
        catch (Exception)
        {
            var fieldInfo = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            ?? throw new MissingFieldException(typeof(TTarget).FullName, fieldName);
            var refl = CreateReflectionTypedSetter<TTarget, TValue>(fieldInfo);
            TypedSetters.TryAdd(key, refl);
            return refl;
        }
    }

    public static Action<TTarget, object> GetObjectSetter<TTarget>(string fieldName)
    {
        var key = ObjectKey(typeof(TTarget), fieldName);
        if (ObjectSetters.TryGetValue(key, out var existing))
        {
            // stored as Action<object, object> - need to return Action<TTarget, object>
            var del = existing!; // Action<object, object>
            return (target, value) => del(target!, value);
        }

        try
        {
            var setter = CreateObjectSetter<TTarget>(fieldName);
            // store as Action<object, object> for uniformity
            Action<object, object> boxed = (t, v) => setter((TTarget)t!, v);
            ObjectSetters.TryAdd(key, boxed);
            return setter;
        }
        catch (Exception)
        {
            var fieldInfo = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            ?? throw new MissingFieldException(typeof(TTarget).FullName, fieldName);
            var refl = CreateReflectionObjectSetter<TTarget>(fieldInfo);
            Action<object, object> boxed = (t, v) => refl((TTarget)t!, v);
            ObjectSetters.TryAdd(key, boxed);
            return refl;
        }
    }

    public static Func<TTarget, TValue> GetTypedGetter<TTarget, TValue>(string fieldName)
    {
        var key = TypedKey(typeof(TTarget), fieldName, typeof(TValue));
        if (TypedGetters.TryGetValue(key, out var existing))
        {
            return (Func<TTarget, TValue>)existing!;
        }

        try
        {
            var getter = CreateTypedGetter<TTarget, TValue>(fieldName);
            TypedGetters.TryAdd(key, getter);
            return getter;
        }
        catch (Exception)
        {
            var fieldInfo = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            ?? throw new MissingFieldException(typeof(TTarget).FullName, fieldName);
            var refl = CreateReflectionTypedGetter<TTarget, TValue>(fieldInfo);
            TypedGetters.TryAdd(key, refl);
            return refl;
        }
    }

    public static Func<TTarget, object> GetObjectGetter<TTarget>(string fieldName)
    {
        var key = ObjectKey(typeof(TTarget), fieldName);
        if (ObjectGetters.TryGetValue(key, out var existing))
        {
            var del = existing!; // Func<object, object>
            return target => del(target!);
        }

        try
        {
            var getter = CreateObjectGetter<TTarget>(fieldName);
            Func<object, object> boxed = t => getter((TTarget)t!);
            ObjectGetters.TryAdd(key, boxed);
            return getter;
        }
        catch (Exception)
        {
            var fieldInfo = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                            ?? throw new MissingFieldException(typeof(TTarget).FullName, fieldName);
            var refl = CreateReflectionObjectGetter<TTarget>(fieldInfo);
            Func<object, object> boxed = t => refl((TTarget)t!);
            ObjectGetters.TryAdd(key, boxed);
            return refl;
        }
    }

    // --- STATIC SETTER CREATORS ---
    private static Action<TValue> CreateStaticTypedSetter<TTarget, TValue>(string fieldName)
    {
        var targetType = typeof(TTarget);
        var fieldInfo = targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? throw new MissingFieldException(targetType.FullName, fieldName);

        var valueParam = Expression.Parameter(typeof(TValue), "value");
        Expression valueExpr = valueParam;
        if (!fieldInfo.FieldType.IsAssignableFrom(typeof(TValue)))
        {
            valueExpr = Expression.Convert(valueParam, fieldInfo.FieldType);
        }

        var fieldExpr = Expression.Field(null, fieldInfo);
        var assign = Expression.Assign(fieldExpr, valueExpr);
        return Expression.Lambda<Action<TValue>>(assign, valueParam).Compile();
    }

    private static Action<object> CreateStaticObjectSetter(Type targetType, string fieldName)
    {
        var fieldInfo = targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? throw new MissingFieldException(targetType.FullName, fieldName);

        var valueParam = Expression.Parameter(typeof(object), "value");
        var converted = Expression.Convert(valueParam, fieldInfo.FieldType);
        var fieldExpr = Expression.Field(null, fieldInfo);
        var assign = Expression.Assign(fieldExpr, converted);
        return Expression.Lambda<Action<object>>(assign, valueParam).Compile();
    }

    // Reflection-based fallback creators for static setters
    private static Action<TValue> CreateReflectionStaticTypedSetter<TTarget, TValue>(FieldInfo fieldInfo)
    {
        return value => fieldInfo.SetValue(null, value);
    }

    private static Action<object> CreateReflectionStaticObjectSetter(FieldInfo fieldInfo)
    {
        return value => fieldInfo.SetValue(null, value);
    }

    // --- STATIC GETTER CREATORS ---
    private static Func<TValue> CreateStaticTypedGetter<TTarget, TValue>(string fieldName)
    {
        var targetType = typeof(TTarget);
        var fieldInfo = targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? throw new MissingFieldException(targetType.FullName, fieldName);

        var fieldExpr = Expression.Field(null, fieldInfo);
        Expression body = Expression.Convert(fieldExpr, typeof(TValue));
        return Expression.Lambda<Func<TValue>>(body).Compile();
    }

    private static Func<object> CreateStaticObjectGetter(Type targetType, string fieldName)
    {
        var fieldInfo = targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        ?? throw new MissingFieldException(targetType.FullName, fieldName);

        var fieldExpr = Expression.Convert(Expression.Field(null, fieldInfo), typeof(object));
        return Expression.Lambda<Func<object>>(fieldExpr).Compile();
    }

    // Reflection-based fallback creators for static getters
    private static Func<TValue> CreateReflectionStaticTypedGetter<TValue>(FieldInfo fieldInfo)
    {
        return () => (TValue)fieldInfo.GetValue(null)!;
    }

    private static Func<object> CreateReflectionStaticObjectGetter(FieldInfo fieldInfo)
    {
        return () => fieldInfo.GetValue(null)!;
    }

    // --- PUBLIC STATIC GET/SET RETRIEVAL (with caching + fallback) ---
    public static Action<TValue> GetStaticTypedSetter<TTarget, TValue>(string fieldName)
    {
        var key = StaticTypedKey(typeof(TTarget), fieldName, typeof(TValue));
        if (StaticTypedSetters.TryGetValue(key, out var existing))
        {
            return (Action<TValue>)existing!;
        }

        try
        {
            var setter = CreateStaticTypedSetter<TTarget, TValue>(fieldName);
            StaticTypedSetters.TryAdd(key, setter);
            return setter;
        }
        catch (Exception)
        {
            var fieldInfo = typeof(TTarget).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            ?? throw new MissingFieldException(typeof(TTarget).FullName, fieldName);
            var refl = CreateReflectionStaticTypedSetter<TTarget, TValue>(fieldInfo);
            StaticTypedSetters.TryAdd(key, refl);
            return refl;
        }
    }

    public static Action<object> GetStaticObjectSetter(Type targetType, string fieldName)
    {
        var key = StaticObjectKey(targetType, fieldName);
        if (StaticObjectSetters.TryGetValue(key, out var existing))
        {
            return existing!;
        }

        try
        {
            var setter = CreateStaticObjectSetter(targetType, fieldName);
            StaticObjectSetters.TryAdd(key, setter);
            return setter;
        }
        catch (Exception)
        {
            var fieldInfo = targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            ?? throw new MissingFieldException(targetType.FullName, fieldName);
            var refl = CreateReflectionStaticObjectSetter(fieldInfo);
            StaticObjectSetters.TryAdd(key, refl);
            return refl;
        }
    }

    public static Func<TValue> GetStaticTypedGetter<TTarget, TValue>(string fieldName)
    {
        var key = StaticTypedKey(typeof(TTarget), fieldName, typeof(TValue));
        if (StaticTypedGetters.TryGetValue(key, out var existing))
        {
            return (Func<TValue>)existing!;
        }

        try
        {
            var getter = CreateStaticTypedGetter<TTarget, TValue>(fieldName);
            StaticTypedGetters.TryAdd(key, getter);
            return getter;
        }
        catch (Exception)
        {
            var fieldInfo = typeof(TTarget).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            ?? throw new MissingFieldException(typeof(TTarget).FullName, fieldName);
            var refl = CreateReflectionStaticTypedGetter<TValue>(fieldInfo);
            StaticTypedGetters.TryAdd(key, refl);
            return refl;
        }
    }

    public static Func<object> GetStaticObjectGetter(Type targetType, string fieldName)
    {
        var key = StaticObjectKey(targetType, fieldName);
        if (StaticObjectGetters.TryGetValue(key, out var existing))
        {
            return existing!;
        }

        try
        {
            var getter = CreateStaticObjectGetter(targetType, fieldName);
            StaticObjectGetters.TryAdd(key, getter);
            return getter;
        }
        catch (Exception)
        {
            var fieldInfo = targetType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            ?? throw new MissingFieldException(targetType.FullName, fieldName);
            var refl = CreateReflectionStaticObjectGetter(fieldInfo);
            StaticObjectGetters.TryAdd(key, refl);
            return refl;
        }
    }

    // Generic setter/getter helpers (extension methods)
    public static void SetField<TTarget, TValue>(this TTarget target, string fieldName, TValue value)
        => GetTypedSetter<TTarget, TValue>(fieldName)(target, value);

    public static void SetField<TTarget>(this TTarget target, string fieldName, object value)
        => GetObjectSetter<TTarget>(fieldName)(target, value);

    public static TValue GetField<TTarget, TValue>(this TTarget target, string fieldName)
        => GetTypedGetter<TTarget, TValue>(fieldName)(target);

    public static object GetField<TTarget>(this TTarget target, string fieldName)
        => GetObjectGetter<TTarget>(fieldName)(target);

    // Static convenience helpers
    public static void SetStaticField<TTarget, TValue>(string fieldName, TValue value)
        => GetStaticTypedSetter<TTarget, TValue>(fieldName)(value);

    public static void SetStaticField(this Type targetType, string fieldName, object value)
        => GetStaticObjectSetter(targetType, fieldName)(value);

    public static TValue GetStaticField<TTarget, TValue>(string fieldName)
        => GetStaticTypedGetter<TTarget, TValue>(fieldName)();

    public static object GetStaticField(this Type targetType, string fieldName)
        => GetStaticObjectGetter(targetType, fieldName)();
}