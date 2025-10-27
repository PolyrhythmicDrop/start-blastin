using System;
using System.Collections.Generic;
using Godot;

namespace Services
{
    /// <summary>
    /// Central registry for game services. Use this to register and retrieve services by type.
    /// </summary>
    public partial class ServiceManager : Node
    {
        /// <summary>
        /// Singleton instance of the ServiceManager. Set in the _Ready method.
        /// </summary>
        public static ServiceManager Instance { get; private set; }

        /// <summary>
        /// Internal dictionary mapping service types to their instances.
        /// </summary>
        private readonly Dictionary<Type, object> _services = new();

        /// <summary>
        /// Called when the node enters the scene tree for the first time. Sets the singleton instance.
        /// </summary>
        public override void _Ready()
        {
            Instance = this;
        }

        /// <summary>
        /// Registers a service instance by its type. If a service of the same type already exists, it will be replaced.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="service">The service instance to register.</param>
        public void RegisterService<T>(T service)
            where T : class
        {
            _services[typeof(T)] = service;
        }

        /// <summary>
        /// Retrieves a registered service by its type.
        /// </summary>
        /// <typeparam name="T">The service type to retrieve.</typeparam>
        /// <returns>The registered service instance if found; otherwise, null.</returns>
        public T GetService<T>()
            where T : class
        {
            return _services.TryGetValue(typeof(T), out var service) ? service as T : null;
        }
    }
}
