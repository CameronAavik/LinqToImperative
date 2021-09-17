using LinqToImperative.Converters.Producers;
using LinqToImperative.ExprEnumerable;

namespace LinqToImperative.Converters
{
    /// <summary>
    /// Extension method for converting arrays to ImperativeQueryable
    /// </summary>
    public static partial class ArrayExtensions
    {
        /// <summary>
        /// Creates an <see cref="ImperativeQueryable{T}"/> from an array.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="arr">The array.</param>
        /// <returns>The queryable object.</returns>
        public static ImperativeQueryable<T> AsImperativeQueryable<T>(this T[] arr) => 
            ImperativeQueryable<T>.Create(arr.AsExprEnumerable());

        /// <summary>
        /// Creates an <see cref="ImperativeQueryable{T}"/> from an array.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="arr">The array.</param>
        /// <returns>The queryable object.</returns>
        internal static IExprEnumerable AsExprEnumerable<T>(this T[] arr) =>
            new LinearExprEnumerable(ArrayProducer.Create(arr));
    }
}
