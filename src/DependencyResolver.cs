using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace auto_dial
{
    /// <summary>
    /// Resolves the correct registration order of services based on their dependencies
    /// using a topological sort algorithm (Kahn's algorithm). It detects and reports circular dependencies.
    /// </summary>
    internal class DependencyResolver
    {
        private readonly List<AutoDialRegistrationBuilder.ServiceImplementation> _implementations;
        // Adjacency list: Key is a dependency, Value is a list of services that depend on it.
        private readonly Dictionary<Type, List<Type>> _dependencyGraph;
        // In-degree of each service: Key is an implementation type, Value is the count of its unresolved dependencies.
        private readonly Dictionary<Type, int> _inDegree;

        public DependencyResolver(List<AutoDialRegistrationBuilder.ServiceImplementation> implementations)
        {
            _implementations = implementations;
            _dependencyGraph = new Dictionary<Type, List<Type>>();
            _inDegree = new Dictionary<Type, int>();

            // Initialize in-degrees for all services that are candidates for registration.
            // Initially, each service has an in-degree of 0.
            foreach (var impl in _implementations)
            {
                _inDegree[impl.ImplementationType] = 0;
            }

            // Build the dependency graph and calculate in-degrees.
            // An edge from A to B means B depends on A (A must be registered before B).
            // We iterate through each service and its constructor parameters to find its dependencies.
            foreach (var impl in _implementations)
            {
                var constructor = impl.ImplementationType.GetConstructors()
                                    .OrderByDescending(c => c.GetParameters().Length)
                                    .FirstOrDefault();

                if (constructor != null)
                {
                    foreach (var parameter in constructor.GetParameters())
                    {
                        var dependencyType = parameter.ParameterType;

                        // Find the implementation that provides this dependency.
                        // This needs to handle both concrete and open generic types.
                        var dependentImpl = _implementations.FirstOrDefault(x => x.InterfaceType == dependencyType || x.ImplementationType == dependencyType);

                        if (dependentImpl != null)
                        {
                            // Add an edge from the dependency to the current service.
                            // This means 'impl.ImplementationType' depends on 'dependentImpl.ImplementationType'.
                            // So, 'dependentImpl.ImplementationType' is a prerequisite for 'impl.ImplementationType'.
                            if (!_dependencyGraph.ContainsKey(dependentImpl.ImplementationType))
                            {
                                _dependencyGraph[dependentImpl.ImplementationType] = new List<Type>();
                            }
                            _dependencyGraph[dependentImpl.ImplementationType].Add(impl.ImplementationType);

                            // Increment the in-degree of the current service, as it has one more dependency.
                            _inDegree[impl.ImplementationType]++;
                        }
                    }
                }
            }
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
                var remainingNodes = _implementations.Where(impl => !sortedList.Any(s => s.ImplementationType == impl.ImplementationType)).Select(impl => impl.ImplementationType.Name).ToList();
                throw new InvalidOperationException($"Circular dependency detected among services: {string.Join(", ", remainingNodes)}. Cannot resolve registration order.");
            }

            return sortedList;
        }
    }
}