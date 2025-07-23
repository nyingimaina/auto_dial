using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace auto_dial
{
    public class AutoDialRegistrationBuilder
    {
        private readonly IServiceCollection services;
        private Assembly assembly;
        private string namespacePrefix;

        private Action<Exception> OnException { get; set; } = (_) => { };

        private HashSet<string> excludedInterfaces = new HashSet<string>();
        
        // Cache to store the types we've already reflected
        private static readonly Dictionary<Tuple<Assembly, string>, List<Type>> ReflectedTypesCache = new Dictionary<Tuple<Assembly, string>, List<Type>>();

        public AutoDialRegistrationBuilder(
            IServiceCollection services)
        {
            this.services = services;
            assembly = Assembly.GetCallingAssembly(); // Use the calling assembly, not the executing one
            namespacePrefix = assembly.GetName().Name!; // Default to the namespace of the executing assembly, can be overridden
        }

        /// <summary>
        /// Allows custom configuration for the assembly to scan.
        /// </summary>
        public AutoDialRegistrationBuilder FromAssemblyOf<T>()
        {
            assembly = Assembly.GetAssembly(typeof(T)) ?? throw new InvalidOperationException($"Could not resolve assembly for {typeof(T).FullName}");
            return this;
        }

        /// <summary>
        /// Allows custom namespace prefix for filtering types.
        /// </summary>
        public AutoDialRegistrationBuilder InNamespaceStartingWith(string namespacePrefix)
        {
            this.namespacePrefix = namespacePrefix;
            return this;
        }

        public AutoDialRegistrationBuilder ExcludeInterface<T>()
        {
            return ExcludeInterface(typeof(T));
        }

        public AutoDialRegistrationBuilder ExcludeInterface(Type type)
        {
            if (type.FullName != null)
            {
                excludedInterfaces.Add(type.FullName); // Use FullName instead of Name
            }
            return this;
        }

        public AutoDialRegistrationBuilder IfExceptionOccurs(Action<Exception> onException)
        {
            OnException = onException;
            return this;
        }

        public AutoDialRegistrationBuilder ExcludeInterfaces(params Type[] types)
        {
            foreach (var type in types)
            {
                ExcludeInterface(type);
            }
            return this;
        }

        public IServiceCollection CompleteAutoRegistration()
        {
            try
            {
                if (assembly == null)
                    throw new InvalidOperationException("Assembly must be specified using FromAssemblyOf<T>.");

                var typesToRegister = GetTypesToRegister();
                var implementations = FindImplementations(typesToRegister);

                // Use DependencyResolver to get the services in the correct registration order.
                var dependencyResolver = new DependencyResolver(implementations);
                var sortedImplementations = dependencyResolver.GetSortedImplementations();

                RegisterServices(sortedImplementations);

                return services;
            }
            catch (Exception ex)
            {
                OnException?.Invoke(new InvalidOperationException("Error during DI registration.", ex));
                throw;
            }
        }

        private List<Type> GetTypesToRegister()
        {
            var cacheKey = Tuple.Create(assembly, namespacePrefix);
            if (!ReflectedTypesCache.TryGetValue(cacheKey, out var types))
            {
                var trimmedNamespacePrefix = namespacePrefix.TrimEnd('.');
                types = assembly.GetTypes()
                    .Where(t => t.Namespace != null && t.Namespace.StartsWith(trimmedNamespacePrefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                ReflectedTypesCache[cacheKey] = types;
            }
            return types;
        }

        private List<ServiceImplementation> FindImplementations(List<Type> types)
        {
            var alreadyRegisteredServices = services
                .Where(s => s.ServiceType.Namespace != null && s.ServiceType.Namespace.StartsWith(namespacePrefix))
                .Select(s => s.ServiceType)
                .ToHashSet();

            var implementations = new List<ServiceImplementation>();

            foreach (var type in types.Where(t => t.IsClass && !t.IsAbstract && !HasExcludeAttribute(t) &&
                                                  t.Namespace != null && t.Namespace.StartsWith(namespacePrefix, StringComparison.OrdinalIgnoreCase)))
            {
                var interfaceType = type.GetInterfaces().FirstOrDefault(IsInterfaceEligible);

                if (interfaceType != null)
                {
                    implementations.Add(new ServiceImplementation(type, interfaceType, GetServiceLifetime(type)));
                }
            }
            return implementations;
        }

        private bool IsInterfaceEligible(Type candidateInterface)
        {
            if (candidateInterface.Namespace == null || !candidateInterface.Namespace.StartsWith(namespacePrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            if (candidateInterface.FullName == null || excludedInterfaces.Contains(candidateInterface.FullName))
                return false;

            return true;
        }

        private void RegisterServices(List<ServiceImplementation> implementations)
        {
            foreach (var impl in implementations)
            {
                switch (impl.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(impl.InterfaceType, impl.ImplementationType);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(impl.InterfaceType, impl.ImplementationType);
                        break;
                    case ServiceLifetime.Scoped:
                    default:
                        services.AddScoped(impl.InterfaceType, impl.ImplementationType);
                        break;
                }
            }
        }

        private ServiceLifetime GetServiceLifetime(Type implementationType)
        {
            var lifetimeAttribute = implementationType
                .GetCustomAttributes(typeof(ServiceLifetimeAttribute), false)
                .FirstOrDefault() as ServiceLifetimeAttribute;

            return lifetimeAttribute?.Lifetime ?? ServiceLifetime.Scoped;
        }

        private bool HasExcludeAttribute(Type implementationType)
        {
            return implementationType.GetCustomAttributes(typeof(ExcludeFromDIAttribute), false).Any();
        }

        internal sealed class ServiceImplementation
        {
            public Type ImplementationType { get; set; }
            public Type InterfaceType { get; set; }
            public ServiceLifetime Lifetime { get; set; }

            public ServiceImplementation(Type implementationType, Type interfaceType, ServiceLifetime lifetime)
            {
                ImplementationType = implementationType;
                InterfaceType = interfaceType;
                Lifetime = lifetime;
            }
        }
    }
}
