using System;
using System.Collections.Generic;

namespace EndlessRooms.Core
{
    /// <summary>
    /// Small hand-rolled service locator used instead of a third-party DI framework
    /// (see DECISIONS.md, 2026-08-04). Keeps cross-system references (e.g. Player code
    /// reaching the <see cref="WorldCommandExecutor"/>) interface-based and swappable
    /// for tests without pulling in an external dependency.
    /// </summary>
    public static class GameServices
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Services[typeof(T)] = service;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var value))
            {
                service = (T)value;
                return true;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            if (TryGet<T>(out var service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} has not been registered.");
        }

        public static void Unregister<T>() where T : class
        {
            Services.Remove(typeof(T));
        }

        /// <summary>Clears all registered services. Intended for test teardown and scene reloads.</summary>
        public static void Clear()
        {
            Services.Clear();
        }
    }
}
