using System;
using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// An <see cref="IExprEnumerable"/> representing how to express a single enumerable
    /// imperatively using expressions.
    /// For nested enumerables, see <see cref="INestedExprEnumerable"/>.
    /// </summary>
    public interface ILinearExprEnumerable : IExprEnumerable
    {
        /// <summary>
        /// Gets an expression of type bool which returns true if the enumerable has more elements
        /// to produce.
        /// </summary>
        Expression HasNext { get; }

        /// <summary>
        /// Expression that initializes the state for the enumerable and then calls the
        /// contination expression.
        /// </summary>
        /// <param name="continuation">Expression to call after initializing.</param>
        /// <returns>The initialization expression.</returns>
        Expression Initialize(Expression continuation);

        /// <summary>
        /// An expression that updates the current element of the enumerable, then calls the
        /// continuation with the current element passed in as a <see cref="ParameterExpression"/>.
        /// </summary>
        /// <param name="continuation">
        /// Function that takes a parameter representing the current element and returns an
        /// expression to invoke after updating the current element.
        /// </param>
        /// <returns>The expression for updating the current element.</returns>
        Expression MoveNext(Func<ParameterExpression, Expression> continuation);
    }
}
