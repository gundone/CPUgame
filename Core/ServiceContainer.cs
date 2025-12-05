using System;
using System.Collections.Generic;

namespace CPUgame.Core;

/// <summary>
/// Simple dependency injection container for managing services.
/// Supports singleton and transient lifetimes.
/// </summary>
public class ServiceContainer
{
    private static ServiceContainer? _instance;
    public static ServiceContainer Instance => _instance ??= new ServiceContainer();

    private readonly Dictionary<Type, ServiceDescriptor> _descriptors = new();
    private readonly Dictionary<Type, object> _singletons = new();

    public static void Reset()
    {
        _instance = new ServiceContainer();
    }

    /// <summary>
    /// Register a singleton service with an instance
    /// </summary>
    public ServiceContainer AddSingleton<TService>(TService instance) where TService : class
    {
        _descriptors[typeof(TService)] = new ServiceDescriptor(typeof(TService), ServiceLifetime.Singleton);
        _singletons[typeof(TService)] = instance;
        return this;
    }

    /// <summary>
    /// Register a singleton service with a factory
    /// </summary>
    public ServiceContainer AddSingleton<TService>(Func<ServiceContainer, TService> factory) where TService : class
    {
        _descriptors[typeof(TService)] = new ServiceDescriptor(typeof(TService), ServiceLifetime.Singleton,
            container => factory(container));
        return this;
    }

    /// <summary>
    /// Register a singleton service with implementation type
    /// </summary>
    public ServiceContainer AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _descriptors[typeof(TService)] = new ServiceDescriptor(typeof(TImplementation), ServiceLifetime.Singleton);
        return this;
    }

    /// <summary>
    /// Register a transient service with a factory
    /// </summary>
    public ServiceContainer AddTransient<TService>(Func<ServiceContainer, TService> factory) where TService : class
    {
        _descriptors[typeof(TService)] = new ServiceDescriptor(typeof(TService), ServiceLifetime.Transient,
            container => factory(container));
        return this;
    }

    /// <summary>
    /// Register a transient service with implementation type
    /// </summary>
    public ServiceContainer AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _descriptors[typeof(TService)] = new ServiceDescriptor(typeof(TImplementation), ServiceLifetime.Transient);
        return this;
    }

    /// <summary>
    /// Get a service instance
    /// </summary>
    public TService Get<TService>() where TService : class
    {
        return (TService)Get(typeof(TService));
    }

    /// <summary>
    /// Try to get a service instance
    /// </summary>
    public TService? TryGet<TService>() where TService : class
    {
        if (!_descriptors.ContainsKey(typeof(TService)))
            return null;
        return Get<TService>();
    }

    /// <summary>
    /// Check if a service is registered
    /// </summary>
    public bool IsRegistered<TService>() where TService : class
    {
        return _descriptors.ContainsKey(typeof(TService));
    }

    private object Get(Type serviceType)
    {
        if (!_descriptors.TryGetValue(serviceType, out var descriptor))
        {
            throw new InvalidOperationException($"Service {serviceType.Name} is not registered");
        }

        if (descriptor.Lifetime == ServiceLifetime.Singleton)
        {
            if (_singletons.TryGetValue(serviceType, out var existing))
            {
                return existing;
            }

            var instance = CreateInstance(descriptor);
            _singletons[serviceType] = instance;
            return instance;
        }

        return CreateInstance(descriptor);
    }

    private object CreateInstance(ServiceDescriptor descriptor)
    {
        if (descriptor.Factory != null)
        {
            return descriptor.Factory(this);
        }

        var constructor = descriptor.ImplementationType.GetConstructors()[0];
        var parameters = constructor.GetParameters();

        if (parameters.Length == 0)
        {
            return Activator.CreateInstance(descriptor.ImplementationType)!;
        }

        var args = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = Get(parameters[i].ParameterType);
        }

        return Activator.CreateInstance(descriptor.ImplementationType, args)!;
    }

    private class ServiceDescriptor
    {
        public Type ImplementationType { get; }
        public ServiceLifetime Lifetime { get; }
        public Func<ServiceContainer, object>? Factory { get; }

        public ServiceDescriptor(Type implementationType, ServiceLifetime lifetime, Func<ServiceContainer, object>? factory = null)
        {
            ImplementationType = implementationType;
            Lifetime = lifetime;
            Factory = factory;
        }
    }

    private enum ServiceLifetime
    {
        Singleton,
        Transient
    }
}
