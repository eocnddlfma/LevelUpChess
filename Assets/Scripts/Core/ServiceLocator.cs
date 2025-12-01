using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelUpChess.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static readonly Dictionary<Type, object> lazyServices = new Dictionary<Type, object>();

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
                // Unity 객체인 경우 파괴되었는지 확인
                if (service is UnityEngine.Object unityObj && unityObj == null)
                {
                    Debug.LogWarning($"[ServiceLocator] Service {type.Name} was destroyed, removing from registry.");
                    services.Remove(type);
                    // 파괴된 경우 자동으로 다시 찾기 시도
                    return FindAndRegister<T>();
                }
                return service as T;
            }
            
            // 등록되지 않은 경우 씬에서 자동으로 찾기 시도
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
