using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToImperative.Internal;

namespace LinqToImperative
{
    /// <summary>
    /// A class for creating <see cref="ImperativeQueryable{T}"/>s.
    /// </summary>
    public static class ImperativeQueryableExtensions
    {
        /// <summary>
        /// Creates a compiled function that when invoked will execute the query.
        /// </summary>
        /// <typeparam name="TResult">The type of the query result.</typeparam>
        /// <param name="query">The query to compile.</param>
        /// <returns>The compiled query as a function.</returns>
        public static Func<TResult> Compile<TResult>(Expression<Func<TResult>> query)
        {
            var executor = new QueryExecutor();
            return executor.Compile<TResult>(query.Body);
        }

        /// <summary>
        /// Creates a compiled function that when invoked will execute the query.
        /// </summary>
        /// <typeparam name="TParam1">The type of the first parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query result.</typeparam>
        /// <param name="query">The query to compile.</param>
        /// <returns>The compiled query as a function.</returns>
        public static Func<TParam1, TResult> Compile<TParam1, TResult>(Expression<Func<TParam1, TResult>> query)
        {
            var executor = new QueryExecutor();
            return executor.Compile<TParam1, TResult>(query.Body, query.Parameters[0]);
        }

        /// <summary>
        /// Creates an <see cref="ImperativeQueryable{T}"/> from an array.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="arr">The array.</param>
        /// <returns>The queryable object.</returns>
        public static ImperativeQueryable<T> AsImperativeQueryable<T>(this T[] arr)
        {
            var arrExpr = Expression.Constant(arr);
            var expression = EnumerableExpressionExtensions.OfArray(arrExpr, typeof(T));
            return Create<T>(expression);
        }

        /// <summary>
        /// Creates an <see cref="ImperativeQueryable{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The queryable object.</returns>
        public static ImperativeQueryable<T> AsImperativeQueryable<T>(this IEnumerable<T> enumerable)
        {
            var enumerableExpr = Expression.Constant(enumerable);
            var expression = EnumerableExpressionExtensions.OfEnumerable(enumerableExpr, typeof(T));
            return Create<T>(expression);
        }

        /// <summary>
        /// Creates an <see cref="ImperativeQueryable{T}"/> from an enumerable expression.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="expression">The enumerable expression.</param>
        /// <returns>The queryable object.</returns>
        private static ImperativeQueryable<T> Create<T>(EnumerableExpression expression)
        {
            var executor = new QueryExecutor();
            var provider = new ImperativeQueryProvider(executor);
            return new ImperativeQueryable<T>(provider, expression);
        }
    }
}
