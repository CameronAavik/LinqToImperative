using LinqToImperative.Converters.Producers;
using LinqToImperative.ExprEnumerable;
using System.Collections.Generic;

namespace LinqToImperative.Converters
{
    /// <summary>
    /// Extension method for converting IEnumerable to ImperativeQueryable
    /// </summary>
    public static partial class EnumerableExtensions
    {
        /// <summary>
        /// Creates an <see cref="ImperativeQueryable{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The queryable object.</returns>
        public static ImperativeQueryable<T> AsImperativeQueryable<T>(this IEnumerable<T> enumerable) =>
            ImperativeQueryable<T>.Create(enumerable.AsExprEnumerable());

        /// <summary>
        /// Creates an <see cref="ImperativeQueryable{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The queryable object.</returns>
        internal static IExprEnumerable AsExprEnumerable<T>(this IEnumerable<T> enumerable) =>
            EnumerableProducer.Create(enumerable).AsExprEnumerable();
    }
}
