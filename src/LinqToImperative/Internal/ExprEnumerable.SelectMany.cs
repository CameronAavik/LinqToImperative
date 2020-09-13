using System;
using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// Class containing the implementation of SelectMany for <see cref="IExprEnumerable"/> objects.
    /// </summary>
    public static partial class ExprEnumerableExtensions
    {
        /// <summary>
        /// Takes an IEnumerable{T}, a lambda of T -> IEnumerable{U} and returns an IEnumerable{U}.
        /// This is also known as a flatmap.
        /// </summary>
        /// <param name="enumerable">The original enumerable to flatmap.</param>
        /// <param name="selector">The flatmapper function.</param>
        /// <returns>The flatmapped enumerable.</returns>
        internal static IExprEnumerable SelectMany(this IExprEnumerable enumerable, LambdaExpression selector)
        {
            Type newType = selector.ReturnType.GetIEnumerableElementType();
            return enumerable.SelectManyRaw(elem => selector.Substitute(elem).ToExprEnumerable(newType), newType);
        }

        /// <summary>
        /// Takes a selector and uses it to flatmap the elements of an enumerable.
        /// </summary>
        /// <param name="enumerable">Enumerable to flatmap.</param>
        /// <param name="selector">The flatmapping function.</param>
        /// <param name="newType">The new element type.</param>
        /// <returns>The flatmapped enumerable.</returns>
        private static IExprEnumerable SelectManyRaw(this IExprEnumerable enumerable, Func<ParameterExpression, IExprEnumerable> selector, Type newType)
        {
            return enumerable switch
            {
                ILinearExprEnumerable linear => new SelectManyExprEnumerable(newType, linear, selector),
                INestedExprEnumerable nested => new SelectManyNestedExprEnumerable(newType, nested, selector),
                _ => throw new Exception("ExprEnumerable is in an invalid state, must be a linear or nested enumerable."),
            };
        }

        /// <summary>
        /// Takes an expression of an enumerable and tries to convert it to an
        /// <see cref="IExprEnumerable"/> which defines how to iterate over that enumerable.
        /// </summary>
        /// <param name="expr">An expression representing the enumerable.</param>
        /// <param name="elementType">The type of the elements of the enumerable.</param>
        /// <returns>The enumerable represented by expressions.</returns>
        private static IExprEnumerable ToExprEnumerable(this Expression expr, Type elementType)
        {
            return expr.Type.IsArray
                ? new ArrayExprEnumerable(expr, elementType)
                : new BaseExprEnumerable(expr, elementType);
        }

        /// <summary>
        /// An <see cref="INestedExprEnumerable"/> representing the result of flatmapping a
        /// <see cref="IExprEnumerable"/>.
        /// </summary>
        private class SelectManyExprEnumerable : INestedExprEnumerable
        {
            private readonly Func<ParameterExpression, IExprEnumerable> selector;

            /// <summary>
            /// Initializes a new instance of the <see cref="SelectManyExprEnumerable"/> class.
            /// </summary>
            /// <param name="elementType">The element type.</param>
            /// <param name="baseEnumerable">The base linear enumerable being flatmapped.</param>
            /// <param name="selector">The flatmapping function.</param>
            public SelectManyExprEnumerable(Type elementType, ILinearExprEnumerable baseEnumerable, Func<ParameterExpression, IExprEnumerable> selector)
            {
                this.ElementType = elementType;
                this.BaseEnumerable = baseEnumerable;
                this.selector = selector;
            }

            /// <inheritdoc/>
            public ILinearExprEnumerable BaseEnumerable { get; }

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public IExprEnumerable GetNestedEnumerable(ParameterExpression element) => this.selector(element);
        }

        /// <summary>
        /// An <see cref="INestedExprEnumerable"/> representing the result of flatmapping another
        /// <see cref="INestedExprEnumerable"/>.
        /// </summary>
        private class SelectManyNestedExprEnumerable : INestedExprEnumerable
        {
            private readonly INestedExprEnumerable baseEnumerable;
            private readonly Func<ParameterExpression, IExprEnumerable> selector;

            /// <summary>
            /// Initializes a new instance of the <see cref="SelectManyNestedExprEnumerable"/> class.
            /// </summary>
            /// <param name="elementType">The element type.</param>
            /// <param name="baseEnumerable">The base nested enumerable being flatmapped.</param>
            /// <param name="selector">The flatmapping function.</param>
            public SelectManyNestedExprEnumerable(Type elementType, INestedExprEnumerable baseEnumerable, Func<ParameterExpression, IExprEnumerable> selector)
            {
                this.ElementType = elementType;
                this.baseEnumerable = baseEnumerable;
                this.selector = selector;
            }

            /// <inheritdoc/>
            public ILinearExprEnumerable BaseEnumerable => this.baseEnumerable.BaseEnumerable;

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public IExprEnumerable GetNestedEnumerable(ParameterExpression element)
                => this.baseEnumerable.GetNestedEnumerable(element).SelectManyRaw(this.selector, this.ElementType);
        }
    }
}
