using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// a class that defines a nested enumerable imperatively using expressions.
    /// </summary>
    public class NestedEnumerableExpression : EnumerableExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NestedEnumerableExpression"/> class.
        /// </summary>
        /// <param name="baseEnumerable">The base enumerable.</param>
        /// <param name="getNestedEnumerable">
        /// Expression that takes elements from the base enumerable and returns nested enumerables.
        /// </param>
        /// <param name="elementType">The type of the element of the enumerable.</param>
        public NestedEnumerableExpression(
            LinearEnumerableExpression baseEnumerable,
            Func<ParameterExpression, EnumerableExpression> getNestedEnumerable,
            Type elementType)
        {
            this.BaseEnumerable = baseEnumerable ?? throw new ArgumentNullException(nameof(baseEnumerable));
            this.GetNestedEnumerable = getNestedEnumerable ?? throw new ArgumentNullException(nameof(getNestedEnumerable));
            this.ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        }

        /// <summary>
        /// Gets an expression of type <see cref="IQueryable{TBase}"/> which represents the
        /// underlying base enumerable that nested enumerables are generated from.
        /// </summary>
        public LinearEnumerableExpression BaseEnumerable { get; init; }

        /// <summary>
        /// Gets a lambda expression of type TBase -> <see cref="IQueryable{TResult}"/>.<br/>
        /// - The single parameter represents an element that was generated by the
        ///   <see cref="BaseEnumerable"/>.
        /// - The expression returns another <see cref="EnumerableExpression"/> which may have a
        ///   different type to the base enumerable.
        /// </summary>
        public Func<ParameterExpression, EnumerableExpression> GetNestedEnumerable { get; init; }

        /// <inheritdoc/>
        public override Type ElementType { get; }
    }
}