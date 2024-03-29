using System;
using System.Collections.Generic;

namespace LinqToImperative.Utils
{
    /// <summary>
    /// Helper class for doing things with types.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Returns the T from the first <see cref="IEnumerable{T}"/> interface that is implemented by the provided type.
        /// </summary>
        /// <param name="type">The type to get the element type from.</param>
        /// <returns>The element type.</returns>
        public static Type GetIEnumerableElementType(this Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType()!;
            }

            Type typeDefinitionToFind = typeof(IEnumerable<>);
            foreach (Type implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == typeDefinitionToFind)
                {
                    return implementedInterface.GenericTypeArguments[0];
                }
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeDefinitionToFind)
            {
                return type.GenericTypeArguments[0];
            }

            throw new ArgumentException("The type does not implement IEnumerable<T>", nameof(type));
        }
    }
}
