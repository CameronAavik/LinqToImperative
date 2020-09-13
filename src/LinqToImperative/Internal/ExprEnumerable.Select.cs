using System;
using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// Class containing the implementation of Select for <see cref="IExprEnumerable"/> objects.
    /// </summary>
    public static partial class ExprEnumerableExtensions
    {
        /// <summary>
        /// Takes a lambda expression that maps the elements of the enumerable and returns an
        /// enumerable of the elements after they were mapped.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="selector">The mapping function.</param>
        /// <returns>The enumerable after mapping.</returns>
        internal static IExprEnumerable Select(this IExprEnumerable enumerable, LambdaExpression selector)
        {
            Expression SelectBody(ParameterExpression curValue, Func<ParameterExpression, Expression> continuation)
            {
                ParameterExpression sVar = Expression.Variable(selector.ReturnType, "s");
                return Expression.Block(
                    new[] { sVar },
                    Expression.Assign(sVar, selector.Substitute(curValue)),
                    continuation(sVar));
            }

            return enumerable.SelectRaw(SelectBody, selector.ReturnType);
        }

        /// <summary>
        /// Takes a selector and uses it to map the elements of an enumerable.
        ///
        /// The selector is a function that takes two parameters:
        /// - currentValue : T (the current value of the enumerable)
        /// - continuation : U -> void (a continuation that processes the new mapped values)
        /// and returns an expression that maps the current element and calls the continuation
        ///
        /// The enumerable that is passed in have elements of type T and the enumerable that is
        /// returned will have elements of type U.
        /// </summary>
        /// <param name="enumerable">The enumerable to map.</param>
        /// <param name="selector">The mapping function.</param>
        /// <param name="newType">The new type of the enumerable.</param>
        /// <returns>The new mapped enumerable.</returns>
        private static IExprEnumerable SelectRaw(
            this IExprEnumerable enumerable,
            Func<ParameterExpression, Func<ParameterExpression, Expression>, Expression> selector,
            Type newType)
        {
            return enumerable switch
            {
                ILinearExprEnumerable linear => new SelectExprEnumerable(linear, selector, newType),
                INestedExprEnumerable nested => new SelectNestedExprEnumerable(nested, selector, newType),
                _ => throw new Exception("ExprEnumerable is in an invalid state, must be a linear or nested enumerable."),
            };
        }

        /// <summary>
        /// An <see cref="ILinearExprEnumerable"/> representing the result of mapping another
        /// <see cref="ILinearExprEnumerable"/>.
        /// </summary>
        private class SelectExprEnumerable : ILinearExprEnumerable
        {
            private readonly ILinearExprEnumerable baseEnumerable;
            private readonly Func<ParameterExpression, Func<ParameterExpression, Expression>, Expression> selector;

            /// <summary>
            /// Initializes a new instance of the <see cref="SelectExprEnumerable"/> class.
            /// </summary>
            /// <param name="baseEnumerable">The original enumerable being mapped.</param>
            /// <param name="selector">The selector function.</param>
            /// <param name="elementType">The new type of the enumerable.</param>
            public SelectExprEnumerable(
                ILinearExprEnumerable baseEnumerable,
                Func<ParameterExpression, Func<ParameterExpression, Expression>, Expression> selector,
                Type elementType)
            {
                this.baseEnumerable = baseEnumerable;
                this.selector = selector;
                this.ElementType = elementType;
            }

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public Expression HasNext => this.baseEnumerable.HasNext;

            /// <inheritdoc/>
            public Expression Initialize(Expression continuation) =>
                this.baseEnumerable.Initialize(continuation);

            /// <inheritdoc/>
            public Expression MoveNext(Func<ParameterExpression, Expression> continuation) =>
                this.baseEnumerable.MoveNext(e => this.selector(e, continuation));
        }

        /// <summary>
        /// An <see cref="INestedExprEnumerable"/> representing the result of mapping another
        /// <see cref="INestedExprEnumerable"/>.
        /// </summary>
        private class SelectNestedExprEnumerable : INestedExprEnumerable
        {
            private readonly INestedExprEnumerable baseEnumerable;
            private readonly Func<ParameterExpression, Func<ParameterExpression, Expression>, Expression> selector;

            /// <summary>
            /// Initializes a new instance of the <see cref="SelectNestedExprEnumerable"/> class.
            /// </summary>
            /// <param name="baseEnumerable">The original enumerable being mapped.</param>
            /// <param name="selector">The selector function.</param>
            /// <param name="elementType">The new type of the enumerable.</param>
            public SelectNestedExprEnumerable(
                INestedExprEnumerable baseEnumerable,
                Func<ParameterExpression, Func<ParameterExpression, Expression>, Expression> selector,
                Type elementType)
            {
                this.baseEnumerable = baseEnumerable;
                this.selector = selector;
                this.ElementType = elementType;
            }

            /// <inheritdoc/>
            public ILinearExprEnumerable BaseEnumerable => this.baseEnumerable.BaseEnumerable;

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public IExprEnumerable GetNestedEnumerable(ParameterExpression element)
                => this.baseEnumerable.GetNestedEnumerable(element).SelectRaw(this.selector, this.ElementType);
        }
    }
}
