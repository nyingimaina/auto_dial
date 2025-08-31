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
        private string[]? namespacePrefixes;

        private Action<Exception> OnException { get; set; } = (_) => { };

        private HashSet<string> excludedInterfaces = new HashSet<string>();

        // New properties for extensible dependency exemption
        private readonly HashSet<Type> _ignoredDependencyTypes = new HashSet<Type>();
        private readonly HashSet<string> _ignoredDependencyNamespaces = new HashSet<string>();
        private readonly List<Func<Type, bool>> _ignoredDependencyPredicates = new List<Func<Type, bool>>();

        // New properties for convention-based registration
        private Func<Type, bool>? _conventionPredicate;
        private ServiceLifetime _conventionDefaultLifetime = ServiceLifetime.Scoped; // Default to Scoped if convention is used
        
        // Cache to store the types we've already reflected
        private static readonly object _cacheLock = new object();

        private static readonly Dictionary<Tuple<Assembly, string>, List<Type>> ReflectedTypesCache = new Dictionary<Tuple<Assembly, string>, List<Type>>();

        public AutoDialRegistrationBuilder(
            IServiceCollection services)
        {
            this.services = services;
            assembly = Assembly.GetCallingAssembly(); // Use the calling assembly, not the executing one
            namespacePrefixes = null; // Default to no namespace filtering
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
        public AutoDialRegistrationBuilder InNamespaceStartingWith(params string[] namespacePrefixes)
        {
            this.namespacePrefixes = namespacePrefixes;
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

        /// <summary>
        /// Ignores a specific type during dependency validation. No error will be thrown if this type is a constructor parameter.
        /// </summary>
        public AutoDialRegistrationBuilder IgnoreDependency<T>()
        {
            _ignoredDependencyTypes.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Ignores a specific type during dependency validation. No error will be thrown if this type is a constructor parameter.
        /// </summary>
        public AutoDialRegistrationBuilder IgnoreDependency(Type type)
        {
            _ignoredDependencyTypes.Add(type);
            return this;
        }

        /// <summary>
        /// Ignores all types within a specified namespace prefix during dependency validation.
        /// </summary>
        public AutoDialRegistrationBuilder IgnoreDependenciesFromNamespace(string namespacePrefix)
        {
            _ignoredDependencyNamespaces.Add(namespacePrefix);
            return this;
        }

        /// <summary>
        /// Ignores types that match a custom predicate during dependency validation.
        /// </summary>
        public AutoDialRegistrationBuilder IgnoreDependencyWhere(Func<Type, bool> predicate)
        {
            _ignoredDependencyPredicates.Add(predicate);
            return this;
        }

        /// <summary>
        /// Configures auto_dial to register services based on a custom convention.
        /// Classes matching the predicate will be registered with the specified default lifetime,
        /// unless they have a [ServiceLifetime] attribute which takes precedence.
        /// </summary>
        /// <param name="conventionPredicate">A predicate to identify types that should be registered by convention.</param>
        /// <param name="defaultLifetime">The default ServiceLifetime to apply to convention-matched types without a [ServiceLifetime] attribute.</param>
        public AutoDialRegistrationBuilder RegisterByConvention(Func<Type, bool> conventionPredicate, ServiceLifetime defaultLifetime = ServiceLifetime.Scoped)
        {
            _conventionPredicate = conventionPredicate;
            _conventionDefaultLifetime = defaultLifetime;
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
                var dependencyResolver = new DependencyResolver(implementations, services, _ignoredDependencyTypes, _ignoredDependencyNamespaces, _ignoredDependencyPredicates);
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
            var cacheKey = Tuple.Create(assembly, namespacePrefixes != null ? string.Join(",", namespacePrefixes) : "");
            lock (_cacheLock)
            {
                if (!ReflectedTypesCache.TryGetValue(cacheKey, out var types))
                {
                    types = assembly.GetTypes()
                        .Where(t => t.Namespace != null && (namespacePrefixes == null || namespacePrefixes.Any(prefix => t.Namespace.StartsWith(prefix.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))))
                        .ToList();
                    ReflectedTypesCache[cacheKey] = types;
                }
                return types;
            }
        }

        private List<ServiceImplementation> FindImplementations(List<Type> types)
        {
            var implementations = new List<ServiceImplementation>();

            // Filter for concrete classes that are not excluded.
            var candidateTypes = types.Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                !HasExcludeAttribute(t) &&
                (namespacePrefixes == null || namespacePrefixes.Any(prefix => t.Namespace != null && t.Namespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            );

            foreach (var type in candidateTypes)
            {
                ServiceLifetime? lifetime = null;

                // 1. Check for explicit [ServiceLifetime] attribute (highest precedence)
                var attribute = (ServiceLifetimeAttribute?)type.GetCustomAttributes(typeof(ServiceLifetimeAttribute), false).FirstOrDefault();
                if (attribute != null)
                {
                    lifetime = attribute.Lifetime;
                }
                // 2. Check for convention if no attribute is present
                else if (_conventionPredicate != null && _conventionPredicate(type))
                {
                    lifetime = _conventionDefaultLifetime;
                }

                // Only proceed if a lifetime has been determined (either by attribute or convention)
                if (lifetime.HasValue)
                {
                    var interfaceType = type.GetInterfaces().FirstOrDefault(IsInterfaceEligible);

                    if (interfaceType != null)
                    {
                        // Register the implementation against its eligible interface.
                        implementations.Add(new ServiceImplementation(type, interfaceType, lifetime.Value));
                    }
                    else
                    {
                        // Handle concrete types without a suitable interface. Register the type itself.
                        implementations.Add(new ServiceImplementation(type, type, lifetime.Value));
                    }
                }
            }
            return implementations;
        }

        private bool IsInterfaceEligible(Type candidateInterface)
        {
            if (candidateInterface.Namespace == null || (namespacePrefixes != null && !namespacePrefixes.Any(prefix => candidateInterface.Namespace.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
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
