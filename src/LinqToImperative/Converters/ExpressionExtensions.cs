using LinqToImperative.Converters.Producers;
using LinqToImperative.ExprEnumerable;
using System;
using System.Linq.Expressions;

namespace LinqToImperative.Converters
{
    /// <summary>
    /// Extension methods for converting expressions to IExprEnumerable
    /// </summary>
    public static partial class ExpressionExtensions
    {
        /// <summary>
        /// Creates an <see cref="IExprEnumerable"/> from an expression representing an IEnumerable.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="elementType">The type of the elements if known.</param>
        /// <returns>The queryable object.</returns>
        internal static IExprEnumerable AsExprEnumerable(this Expression expression, Type? elementType = null)
        {
            if (expression.Type.IsArray)
            {
                return new ArrayProducer(expression, elementType).AsExprEnumerable();
            }
            else
            {
                return new EnumerableProducer(expression, elementType).AsExprEnumerable();
            }
        }
    }
}
