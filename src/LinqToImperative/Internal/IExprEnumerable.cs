using System;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// An interface that captures a set of expressions that can be used to convert an expression
    /// of an enumerable into imperative expressions.
    /// For a linear enumerable, use <see cref="ILinearExprEnumerable"/>.
    /// For nested enumerables, use <see cref="INestedExprEnumerable"/>.
    /// </summary>
    public interface IExprEnumerable
    {
        /// <summary>
        /// Gets the type of the elements in the enumerable.
        /// </summary>
        Type ElementType { get; }
    }
}
