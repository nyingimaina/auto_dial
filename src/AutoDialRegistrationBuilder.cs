using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace auto_dial
{
    public class AutoDialRegistrationBuilder
    {
        private readonly IServiceCollection services;
        private Assembly assembly;
        private string namespacePrefix;

        private HashSet<string> excludedInterfaces = new HashSet<string>();

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
                excludedInterfaces.Add(type.Name);
            }
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
        /// Performs the actual registration based on the configured options.
        /// </summary>
        public IServiceCollection CompleteAutoRegistration()
        {
            // Validate that the assembly has been set
            if (assembly == null)
                throw new InvalidOperationException("Assembly must be specified using FromAssemblyOf<T>.");

            var types = assembly.GetTypes()
                .Where(t =>
                {
                    if (t.Namespace != null)
                    {
                        return t.Namespace.StartsWith(namespacePrefix);
                    }
                    return false;
                })
                .ToList();

            var targetServicesFilteredByNamespacePrefix = services
                .Where(candidate => candidate.ServiceType.Namespace != null &&
                        candidate.ServiceType.Namespace.StartsWith(namespacePrefix))
                .ToList();

            var implementations = types.Where(t => t.IsClass && !t.IsAbstract)
                .Select(t => new
                {
                    Implementation = t,
                    Interface = t.GetInterfaces().FirstOrDefault(candidateInterface =>
                    {
                        var matchesNamespace = candidateInterface.Namespace != null &&
                            candidateInterface.Namespace.StartsWith(namespacePrefix);
                        if (matchesNamespace == false)
                        {
                            return false;
                        }

                        var candidateInterfaceName = candidateInterface.Name;
                        var isExcluded = excludedInterfaces.Contains(candidateInterfaceName);
                        return isExcluded == false;
                    }),
                    Lifetime = GetServiceLifetime(t), // Determine the lifetime from the class attributes
                    ExcludeFromDI = HasExcludeAttribute(t) // Check if the class should be excluded
                })
                .ToList();

            implementations = implementations
                .Where(x =>
                {
                    bool isValidCandidate = x.Interface != null && !x.ExcludeFromDI;
                    if (isValidCandidate)
                    {
                        var isAlreadyRegistered = targetServicesFilteredByNamespacePrefix.Any((registeredService) =>
                        {
                            return registeredService.ServiceType == x.Interface;
                        });
                        if (isAlreadyRegistered == false)
                        {
                            // If the service isn't already registered, log a message
                            Console.WriteLine($"Service not registered : {x.Interface!.Name}");
                            return true;
                        }
                    }
                    return false;
                })
                .ToList();

            foreach (var implementation in implementations)
            {
                if (implementation == null || implementation.Interface == null)
                {
                    continue;
                }

                // Register based on the lifetime specified by the attribute
                switch (implementation.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(implementation.Interface, implementation.Implementation);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(implementation.Interface, implementation.Implementation);
                        break;
                    case ServiceLifetime.Scoped:
                    default:
                        services.AddScoped(implementation.Interface, implementation.Implementation);
                        break;
                }
            }
            return services;
        }

        /// <summary>
        /// Retrieves the service lifetime from the [ServiceLifetime] attribute, defaults to Scoped.
        /// </summary>
        private ServiceLifetime GetServiceLifetime(Type implementationType)
        {
            var lifetimeAttribute = implementationType
                .GetCustomAttributes(typeof(ServiceLifetimeAttribute), false)
                .FirstOrDefault() as ServiceLifetimeAttribute;

            return lifetimeAttribute?.Lifetime ?? ServiceLifetime.Scoped; // Default to Scoped if no attribute is found
        }

        /// <summary>
        /// Determines if a class should be excluded from DI registration using the [ExcludeFromDI] attribute.
        /// </summary>
        private bool HasExcludeAttribute(Type implementationType)
        {
            return implementationType.GetCustomAttributes(typeof(ExcludeFromDIAttribute), false).Any();
        }
    }
}
