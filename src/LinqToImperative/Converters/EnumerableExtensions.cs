using LinqToImperative.Converters.Producers;
using LinqToImperative.ExprEnumerable;
using LinqToImperative.QueryTree;
using System.Collections.Generic;
using System.Linq.Expressions;

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
        public static ImperativeQueryable<T> AsImperativeQueryable<T>(this IEnumerable<T> enumerable)
        {
            var param = Expression.Parameter(typeof(IEnumerable<T>), "enumerable");
            var streamQuery = new QueryTypes.ExpressionBackedStreamQuery<T>(param);
            var contextParameter = new ContextParameter(param, enumerable);
            return new ImperativeQueryable<T>(streamQuery, QueryContext.WithRootParameter(contextParameter));
        }
    }
}
