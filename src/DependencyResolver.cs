using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace auto_dial
{
    /// <summary>
    /// Resolves the correct registration order of services based on their dependencies
    /// using a topological sort algorithm (Kahn's algorithm). It detects and reports circular dependencies.
    /// </summary>
    internal class DependencyResolver
    {
        private readonly List<AutoDialRegistrationBuilder.ServiceImplementation> _implementations;
        private readonly IServiceCollection _existingServices;
        private readonly HashSet<Type> _userIgnoredDependencyTypes;
        private readonly HashSet<string> _userIgnoredDependencyNamespaces;
        private readonly List<Func<Type, bool>> _userIgnoredDependencyPredicates;
        // Adjacency list: Key is a dependency, Value is a list of services that depend on it.
        private readonly Dictionary<Type, List<Type>> _dependencyGraph;
        // In-degree of each service: Key is an implementation type, Value is the count of its unresolved dependencies.
        private readonly Dictionary<Type, int> _inDegree;

        public DependencyResolver(List<AutoDialRegistrationBuilder.ServiceImplementation> implementations, IServiceCollection existingServices, HashSet<Type> userIgnoredDependencyTypes, HashSet<string> userIgnoredDependencyNamespaces, List<Func<Type, bool>> userIgnoredDependencyPredicates)
        {
            _implementations = implementations;
            _existingServices = existingServices;
            _userIgnoredDependencyTypes = userIgnoredDependencyTypes;
            _userIgnoredDependencyNamespaces = userIgnoredDependencyNamespaces;
            _userIgnoredDependencyPredicates = userIgnoredDependencyPredicates;
            _dependencyGraph = new Dictionary<Type, List<Type>>();
            _inDegree = new Dictionary<Type, int>();

            var implementationTypes = new HashSet<Type>(implementations.Select(i => i.ImplementationType));
            var interfaceToImplementationMap = implementations.ToDictionary(i => i.InterfaceType, i => i.ImplementationType);
            var knownExternalTypes = new HashSet<Type>(_existingServices.Select(s => s.ServiceType));

            foreach (var impl in _implementations)
            {
                _inDegree[impl.ImplementationType] = 0;
            }

            foreach (var impl in _implementations)
            {
                var constructor = impl.ImplementationType.GetConstructors()
                                    .OrderByDescending(c => c.GetParameters().Length)
                                    .FirstOrDefault();

                if (constructor == null) continue;

                foreach (var parameter in constructor.GetParameters())
                {
                    var dependencyType = parameter.ParameterType;
                    Type? dependentImplType = null;

                    // Check if the dependency is registered as an interface or a concrete type within the auto-dial batch.
                    if (interfaceToImplementationMap.ContainsKey(dependencyType))
                    {
                        dependentImplType = interfaceToImplementationMap[dependencyType];
                    }
                    else if (implementationTypes.Contains(dependencyType))
                    {
                        dependentImplType = dependencyType;
                    }

                    if (dependentImplType != null)
                    {
                        if (!_dependencyGraph.ContainsKey(dependentImplType))
                        {
                            _dependencyGraph[dependentImplType] = new List<Type>();
                        }
                        _dependencyGraph[dependentImplType].Add(impl.ImplementationType);
                        _inDegree[impl.ImplementationType]++;
                    }
                    else if (!IsExempt(dependencyType, knownExternalTypes))
                    {
                        // This is where the unregistered dependency is detected.
                        throw new InvalidOperationException(
                            $"auto_dial Error: Cannot resolve dependency '{(dependencyType.IsInterface ? dependencyType.Name : dependencyType.FullName)}' for the constructor of class '{impl.ImplementationType.Name}'. " +
                            "Please ensure that the implementation for this service is decorated with the [ServiceLifetime] attribute and is included in the assembly/namespace scan, or that it has been registered manually before calling AddAutoDial().");
                    }
                    // If the type is in knownExternalTypes, we assume it's valid and do nothing.
                }
            }
        }

        private bool IsExempt(Type type, HashSet<Type> knownExternalTypes)
        {
            // 1. Check user-defined exemptions (highest precedence)
            if (_userIgnoredDependencyTypes.Contains(type)) return true;
            if (type.Namespace != null && _userIgnoredDependencyNamespaces.Any(prefix => type.Namespace.StartsWith(prefix))) return true;
            if (_userIgnoredDependencyPredicates.Any(predicate => predicate(type))) return true;

            // 2. Check if already registered manually
            if (knownExternalTypes.Contains(type)) return true;

            // 3. Check auto_dial's default framework exemptions
            // Primitive types and string
            if (type.IsPrimitive || type == typeof(string)) return true;

            // Common framework types by namespace
            var ns = type.Namespace;
            if (ns != null)
            {
                if (ns.StartsWith("Microsoft.Extensions.Logging")) return true;
                if (ns.StartsWith("Microsoft.Extensions.Options")) return true;
                if (ns.StartsWith("Microsoft.Extensions.Configuration")) return true;
                if (ns.StartsWith("Microsoft.Extensions.Hosting")) return true;
                if (ns.StartsWith("Microsoft.Extensions.Http")) return true;
            }

            // Specific common types (e.g., IServiceProvider, IServiceScopeFactory, IEnumerable<T>)
            if (type == typeof(IServiceProvider) || type == typeof(IServiceScopeFactory)) return true;

            // Open generics that are commonly resolved by the framework
            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                if (genericTypeDefinition == null) return false; // Should not happen for IsGenericType
                if (genericTypeDefinition == typeof(IEnumerable<>)) return true;
                if (genericTypeDefinition == typeof(IOptions<>)) return true;
                if (genericTypeDefinition == typeof(IOptionsSnapshot<>)) return true;
                if (genericTypeDefinition == typeof(IOptionsMonitor<>)) return true;
                if (genericTypeDefinition == typeof(ILogger<>)) return true;
            }

            return false;
        }

        /// <summary>
        /// Performs a topological sort on the service dependency graph using Kahn's algorithm.
        /// </summary>
        /// <returns>A list of service implementations in the correct registration order.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a circular dependency is detected.</exception>
        public List<AutoDialRegistrationBuilder.ServiceImplementation> GetSortedImplementations()
        {
            var sortedList = new List<AutoDialRegistrationBuilder.ServiceImplementation>();
            var queue = new Queue<Type>();

            // Initialize the queue with all services that have no incoming dependencies (in-degree of 0).
            // These are the services that can be registered first.
            foreach (var entry in _inDegree)
            {
                if (entry.Value == 0)
                {
                    queue.Enqueue(entry.Key);
                }
            }

            // Process nodes in topological order.
            // As services are added to the sorted list, their outgoing edges are 'removed' by decrementing
            // the in-degree of their dependents. When a dependent's in-degree becomes 0, it's added to the queue.
            while (queue.Any())
            {
                var currentImplType = queue.Dequeue();
                var currentImpl = _implementations.First(x => x.ImplementationType == currentImplType);
                sortedList.Add(currentImpl);

                // For each service that depends on the current service (its neighbors in the dependency graph).
                // If currentImplType is a dependency for other services, those services' in-degrees are decremented.
                if (_dependencyGraph.TryGetValue(currentImplType, out var dependents))
                {
                    foreach (var dependentType in dependents)
                    {
                        _inDegree[dependentType]--; // Decrement in-degree of the service that depends on currentImplType

                        if (_inDegree[dependentType] == 0)
                        {
                            queue.Enqueue(dependentType);
                        }
                    }
                }
            }

            // If the number of services in the sorted list is less than the total number of implementations,
            // it indicates that a cycle exists in the dependency graph.
            if (sortedList.Count != _implementations.Count)
            {
                var cycle = FindCycle();
                throw new InvalidOperationException($"auto_dial Error: A circular dependency was detected. The registration order cannot be determined. Dependency chain: {string.Join(" -> ", cycle)}");
            }

            return sortedList;
        }

        private List<string> FindCycle()
        {
            var remainingNodes = _inDegree.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToHashSet();
            if (!remainingNodes.Any()) return new List<string>();

            var path = new List<Type>();
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();

            Func<Type, bool> visit = null;
            visit = (node) =>
            {
                visited.Add(node);
                recursionStack.Add(node);
                path.Add(node);

                if (_dependencyGraph.ContainsKey(node))
                {
                    foreach (var neighbor in _dependencyGraph[node])
                    {
                        if (recursionStack.Contains(neighbor))
                        {
                            path.Add(neighbor);
                            return true; // Cycle detected
                        }
                        if (!visited.Contains(neighbor))
                        {
                            if (visit(neighbor)) return true;
                        }
                    }
                }

                recursionStack.Remove(node);
                path.RemoveAt(path.Count - 1);
                return false;
            };

            foreach (var node in remainingNodes)
            {
                if (!visited.Contains(node))
                {
                    if (visit(node))
                    {
                        // Format the cycle path
                        var cycleStartIndex = path.IndexOf(path.Last());
                        return path.Skip(cycleStartIndex).Select(t => t.Name).ToList();
                    }
                }
            }

            // Fallback for complex graphs where a simple path isn't found, though the topological sort already failed.
            return remainingNodes.Select(t => t.Name).ToList();
        }
    }
}