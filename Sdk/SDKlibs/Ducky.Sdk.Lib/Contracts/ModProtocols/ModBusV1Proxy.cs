using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Cysharp.Threading.Tasks;

namespace Ducky.Sdk.Contracts;

/// <summary>
/// ModHttpV1 proxy for efficient reflection-based calls without direct dependency on the original library.
/// </summary>
public class ModHttpV1Proxy
{
    private readonly object _hubInstance;
    private readonly Action<object, string, Func<string, string, string, UniTask>> _registerClientDelegate;
    private readonly Action<object, string> _unregisterClientDelegate;
    private readonly Func<object, string, string, string, string, UniTask> _notifyDelegate;
    private readonly Func<object, IReadOnlyList<string>> _getModIdsDelegate;

    public ModHttpV1Proxy(object hubInstance)
    {
        var type = hubInstance.GetType();
        _hubInstance = hubInstance;

        // RegisterClient(string, Func<string, string, string, UniTask>)
        var regMethod = type.GetMethod("RegisterClient", BindingFlags.Public | BindingFlags.Instance, null,
            new[] { typeof(string), typeof(Func<string, string, string, UniTask>) }, null);
        var regInstance = Expression.Parameter(typeof(object), "instance");
        var regModId = Expression.Parameter(typeof(string), "modId");
        var regCallback = Expression.Parameter(typeof(Func<string, string, string, UniTask>), "callback");
        var regCall = Expression.Call(
            Expression.Convert(regInstance, type),
            regMethod,
            regModId,
            regCallback
        );
        _registerClientDelegate = Expression
            .Lambda<Action<object, string, Func<string, string, string, UniTask>>>(regCall, regInstance, regModId,
                regCallback)
            .Compile();

        // UnregisterClient(string)
        var unregMethod = type.GetMethod("UnregisterClient", BindingFlags.Public | BindingFlags.Instance, null,
            new[] { typeof(string) }, null);
        var unregInstance = Expression.Parameter(typeof(object), "instance");
        var unregModId = Expression.Parameter(typeof(string), "modId");
        var unregCall = Expression.Call(
            Expression.Convert(unregInstance, type),
            unregMethod,
            unregModId
        );
        _unregisterClientDelegate = Expression.Lambda<Action<object, string>>(unregCall, unregInstance, unregModId)
            .Compile();

        // Notify(string, string, string, string) : UniTask
        var notifyMethod = type.GetMethod("Notify", BindingFlags.Public | BindingFlags.Instance, null,
            new[] { typeof(string), typeof(string), typeof(string), typeof(string) }, null);
        var notifyInstance = Expression.Parameter(typeof(object), "instance");
        var notifyFromModId = Expression.Parameter(typeof(string), "fromModId");
        var notifyToModId = Expression.Parameter(typeof(string), "toModId");
        var notifyContentType = Expression.Parameter(typeof(string), "contentType");
        var notifyBody = Expression.Parameter(typeof(string), "body");
        var notifyCall = Expression.Call(
            Expression.Convert(notifyInstance, type),
            notifyMethod,
            notifyFromModId,
            notifyToModId,
            notifyContentType,
            notifyBody
        );
        _notifyDelegate = Expression
            .Lambda<Func<object, string, string, string, string, UniTask>>(notifyCall, notifyInstance, notifyFromModId,
                notifyToModId, notifyContentType, notifyBody).Compile();

        // ModIds property getter (IReadOnlyList<string>)
        var modIdsProperty = type.GetProperty("ModIds", BindingFlags.Public | BindingFlags.Instance);
        var modIdsInstance = Expression.Parameter(typeof(object), "instance");
        var modIdsGet = Expression.Property(
            Expression.Convert(modIdsInstance, type),
            modIdsProperty
        );
        _getModIdsDelegate = Expression
            .Lambda<Func<object, IReadOnlyList<string>>>(modIdsGet, modIdsInstance)
            .Compile();
    }

    // 高效注册委托（表达式编译）
    public void RegisterClient(string modId, Func<string, string, string, UniTask> callback)
    {
        _registerClientDelegate(_hubInstance, modId, callback);
    }

    public void UnregisterClient(string modId)
    {
        _unregisterClientDelegate(_hubInstance, modId);
    }

    public UniTask Notify(string fromModId, string toModId, string contentType, string body)
    {
        return _notifyDelegate(_hubInstance, fromModId, toModId, contentType, body);
    }

    public IReadOnlyList<string> GetModIds()
    {
        return _getModIdsDelegate(_hubInstance);
    }

    // 工厂方法：通过 GameObject 名称查找 ModHttpV1 实例
    public static ModHttpV1Proxy CreateFromSingleton()
    {
        var go = UnityEngine.GameObject.Find("ModHttpV1");
        if (go == null) throw new InvalidOperationException("未找到 ModHttpV1 GameObject");

        // 通过反射获取 ModHttpV1 类型并获取组件
        var components = go.GetComponents<UnityEngine.MonoBehaviour>();
        object? hub = null;
        foreach (var component in components)
        {
            if (component.GetType().Name == "ModHttpV1")
            {
                hub = component;
                break;
            }
        }

        if (hub == null) throw new InvalidOperationException("ModHttpV1 组件未挂载");
        return new ModHttpV1Proxy(hub);
    }
}
