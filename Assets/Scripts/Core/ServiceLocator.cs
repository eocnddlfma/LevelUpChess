using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelUpChess.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service {type.Name} is already registered. Replacing...");
            }
            services[type] = service;
            Debug.Log($"[ServiceLocator] Registered: {type.Name}");
        }

        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);
            if (services.ContainsKey(type))
            {
                services.Remove(type);
                Debug.Log($"[ServiceLocator] Unregistered: {type.Name}");
            }
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (services.TryGetValue(type, out var service))
            {
                if (service is UnityEngine.Object unityObj && unityObj == null)
                {
                    Debug.LogWarning($"[ServiceLocator] Service {type.Name} was destroyed, removing from registry.");
                    services.Remove(type);
                    return FindAndRegister<T>();
                }
                return service as T;
            }
            
            return FindAndRegister<T>();
        }
        
        private static T FindAndRegister<T>() where T : class
        {
            var type = typeof(T);
            
            // MonoBehaviour인 경우 씬에서 찾기
            if (typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                var found = UnityEngine.Object.FindFirstObjectByType(type) as T;
                if (found != null)
                {
                    services[type] = found;
                    Debug.Log($"[ServiceLocator] Auto-registered {type.Name} from scene.");
                    return found;
                }
            }
            
            Debug.LogWarning($"[ServiceLocator] Service {type.Name} not found!");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }

        public static bool Has<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        public static void Clear()
        {
            services.Clear();
            Debug.Log("[ServiceLocator] All services cleared");
        }
    }
}
