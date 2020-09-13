using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// An interface representing an expression builder for nested enumerables.
    /// This would be used to represent SelectMany (aka flatmap).
    /// </summary>
    public interface INestedExprEnumerable : IExprEnumerable
    {
        /// <summary>
        /// Gets the base linear enumerable expression.
        /// This enumerable generates elements of type T.
        /// </summary>
        ILinearExprEnumerable BaseEnumerable { get; }

        /// <summary>
        /// Takes an element of type T and returns an enumerable that generates elements of type U.
        /// This returned enumerable can be linear or nested.
        /// </summary>
        /// <param name="element">A parameter expression with type T.</param>
        /// <returns>The enumerable expression.</returns>
        IExprEnumerable GetNestedEnumerable(ParameterExpression element);
    }
}
