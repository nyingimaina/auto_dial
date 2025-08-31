using System;
using System.Linq;
using System.Reflection;

namespace auto_dial
{
    /// <summary>
    /// Provides a collection of common predicates for filtering types based on various criteria.
    /// These predicates can be used with methods like `RegisterByConvention` and `IgnoreDependencyWhere`.
    /// </summary>
    public static class TypeFilters
    {
        /// <summary>
        /// Creates a predicate that returns true if a type inherits from or implements a specified base type or interface.
        /// Handles concrete classes, interfaces, and generic type definitions.
        /// </summary>
        /// <param name="baseType">The base type or interface to check against.</param>
        /// <returns>A predicate function.</returns>
        public static Func<Type, bool> InheritsOrImplements(Type baseType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));

            return type => InheritsOrImplements(type, baseType);
        }

        /// <summary>
        /// Creates a predicate that returns true if a type inherits from or implements a specified base type or interface.
        /// Generic overload for convenience.
        /// </summary>
        /// <typeparam name="TBase">The base type or interface to check against.</typeparam>
        /// <returns>A predicate function.</returns>
        public static Func<Type, bool> InheritsOrImplements<TBase>()
        {
            return InheritsOrImplements(typeof(TBase));
        }

        /// <summary>
        /// Private helper for recursive inheritance/implementation check.
        /// </summary>
        private static bool InheritsOrImplements(Type type, Type baseType)
        {
            if (type == null) return false;

            // Direct assignment or inheritance
            if (baseType.IsAssignableFrom(type)) return true;

            // Handle generic type definitions (e.g., checking if type implements IMyInterface<T>)
            if (baseType.IsGenericTypeDefinition)
            {
                // Check interfaces
                if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == baseType))
                    return true;

                // Check if the type itself is a generic type and matches the definition
                if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
                    return true;

                // Check base types recursively for generic definitions
                if (type.BaseType != null && InheritsOrImplements(type.BaseType, baseType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a predicate that returns true if a type implements a specified interface.
        /// Handles generic interface definitions.
        /// </summary>
        /// <param name="interfaceType">The interface type to check against.</param>
        /// <returns>A predicate function.</returns>
        public static Func<Type, bool> Implements(Type interfaceType)
        {
            if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
            if (!interfaceType.IsInterface) throw new ArgumentException("Type must be an interface.", nameof(interfaceType));

            return type =>
            {
                if (type == null) return false;
                return type.GetInterfaces().Any(i =>
                    (i == interfaceType) || (i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType));
            };
        }

        /// <summary>
        /// Creates a predicate that returns true if a type implements a specified interface.
        /// Generic overload for convenience.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to check against.</typeparam>
        /// <returns>A predicate function.</returns>
        public static Func<Type, bool> Implements<TInterface>()
        {
            return Implements(typeof(TInterface));
        }

        /// <summary>
        /// Creates a predicate that returns true if a type is decorated with a specified attribute.
        /// </summary>
        /// <param name="attributeType">The attribute type to check for.</param>
        /// <returns>A predicate function.</returns>
        public static Func<Type, bool> HasAttribute(Type attributeType)
        {
            if (attributeType == null) throw new ArgumentNullException(nameof(attributeType));
            if (!typeof(Attribute).IsAssignableFrom(attributeType)) throw new ArgumentException("Type must be an attribute.", nameof(attributeType));

            return type =>
            {
                if (type == null) return false;
                return type.GetCustomAttributes(attributeType, true).Any();
            };
        }

        /// <summary>
        /// Creates a predicate that returns true if a type is decorated with a specified attribute.
        /// Generic overload for convenience.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute type to check for.</typeparam>
        /// <returns>A predicate function.</returns>
        public static Func<Type, bool> HasAttribute<TAttribute>() where TAttribute : Attribute
        {
            return HasAttribute(typeof(TAttribute));
        }

        /// <summary>
        /// Creates a predicate that returns true if a type's name ends with the specified suffix.
        /// </summary>
        /// <param name="suffix">The suffix to check for.</param>
        /// <param name="comparisonType">The string comparison type.</param>
        /// <returns>A predicate function.</returns>
        public static Func<Type, bool> EndsWith(string suffix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (suffix == null) throw new ArgumentNullException(nameof(suffix));

            return type =>
            {
                if (type == null) return false;
                return type.Name.EndsWith(suffix, comparisonType);
            };
        }

        /// <summary>
        /// Creates a predicate that returns true if a type's name starts with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to check for.</param>
        /// <param name="comparisonType">The string comparison type.</param>
        /// <returns>A predicate function.</returns>
        public static Func<Type, bool> StartsWith(string prefix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (prefix == null) throw new ArgumentNullException(nameof(prefix));

            return type =>
            {
                if (type == null) return false;
                return type.Name.StartsWith(prefix, comparisonType);
            };
        }

        /// <summary>
        /// Creates a predicate that returns true if a type's namespace starts with the specified prefix.
        /// </summary>
        /// <param name="namespacePrefix">The namespace prefix to check for.</param>
        /// <param name="comparisonType">The string comparison type.</param>
        /// <returns>A predicate function.</returns>
        public static Func<Type, bool> IsInNamespace(string namespacePrefix, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (namespacePrefix == null) throw new ArgumentNullException(nameof(namespacePrefix));

            return type =>
            {
                if (type == null || type.Namespace == null) return false;
                return type.Namespace.StartsWith(namespacePrefix, comparisonType);
            };
        }
    }
}
