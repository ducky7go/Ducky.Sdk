using UnityEngine;
using Ducky.Sdk.Logging;
using static UnityEngine.Object;

namespace Ducky.Sdk.Contracts.ModProtocols;

/// <summary>
/// Utility class for managing MessageHub host detection and lifecycle
/// </summary>
internal static class MessageHubManager
{
    private static object? _hostInstance;
    private static readonly object _lock = new();

    /// <summary>
    /// Check if MessageHub host is already running
    /// </summary>
    /// <returns>true if host is found, false otherwise</returns>
    public static bool IsHostRunning()
    {
        lock (_lock)
        {
            if (_hostInstance != null)
                return true;

            // Look for existing host in scene by name
            var existingGO = GameObject.Find(ModHttpV1.HubGameObjectName);
            if (existingGO != null)
            {
                // Get the MonoBehaviour component by type name
                var components = existingGO.GetComponents<MonoBehaviour>();
                foreach (var component in components)
                {
                    if (component != null && component.GetType().Name == "ModHttpV1")
                    {
                        _hostInstance = component;
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Create and start MessageHub host
    /// </summary>
    /// <returns>The created host instance or null if failed</returns>
    public static object? StartHost()
    {
        lock (_lock)
        {
            if (_hostInstance != null)
                return _hostInstance;

            // Check again for existing host
            if (IsHostRunning())
                return _hostInstance;

            // Create new host GameObject
            var hostGO = new GameObject(ModHttpV1.HubGameObjectName);
            var hostComponent = hostGO.AddComponent<ModHttpV1>();
            _hostInstance = hostComponent;

            DontDestroyOnLoad(hostGO);

            // Call Active method using reflection
            var activeMethod = hostComponent.GetType().GetMethod("Active");
            activeMethod?.Invoke(hostComponent, null);

            Log.Info("MessageHubManager: Created new MessageHub host");
            return _hostInstance;
        }
    }

    /// <summary>
    /// Get current host instance
    /// </summary>
    /// <returns>Current host instance or null if not running</returns>
    public static object? GetCurrentHost()
    {
        lock (_lock)
        {
            if (_hostInstance != null)
                return _hostInstance;

            // Try to find existing host
            IsHostRunning();
            return _hostInstance;
        }
    }
}
