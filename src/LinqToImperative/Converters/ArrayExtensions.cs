using LinqToImperative.Converters.Producers;
using LinqToImperative.ExprEnumerable;
using LinqToImperative.QueryTree;
using System.Linq.Expressions;

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
        public static ImperativeQueryable<T> AsImperativeQueryable<T>(this T[] arr)
        {
            var param = Expression.Parameter(typeof(T[]), "arr");
            var streamQuery = new QueryTypes.ExpressionBackedStreamQuery<T>(param);
            var contextParameter = new ContextParameter(param, arr);
            return new ImperativeQueryable<T>(streamQuery, QueryContext.WithRootParameter(contextParameter));
        }
    }
}
