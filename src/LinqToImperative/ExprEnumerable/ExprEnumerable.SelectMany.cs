using LinqToImperative.Converters;
using LinqToImperative.Producers;
using LinqToImperative.Utils;
using System;
using System.Linq.Expressions;

namespace LinqToImperative.ExprEnumerable
{
    /// <summary>
    /// Implementation of LINQ SelectMany for IExprEnumerable.
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
            return enumerable switch
            {
                LinearExprEnumerable linear => new SelectManyLinearExprEnumerable(linear, selector),
                INestedExprEnumerable nested => new SelectManyNestedExprEnumerable(nested, selector),
                _ => throw new ArgumentException("Must be a linear or nested enumerable."),
            };
        }

        /// <summary>
        /// An nested enumerable that is produced as the result of performing a SelectMany on a linear enumerable.
        /// </summary>
        internal readonly struct SelectManyLinearExprEnumerable : INestedExprEnumerable
        {
            private readonly LambdaExpression selector;

            /// <summary>
            /// Creates a new instance of <see cref="SelectManyLinearExprEnumerable"/>.
            /// </summary>
            /// <param name="baseEnumerable">The base enumerable being wrapped.</param>
            /// <param name="selector">A function which takes an element and produces an enumerable of elements.</param>
            /// <param name="elementType">The new element type of the producer.</param>
            public SelectManyLinearExprEnumerable(LinearExprEnumerable baseEnumerable, LambdaExpression selector, Type? elementType = null)
            {
                this.selector = selector;
                BaseProducer = baseEnumerable.Producer;
                ElementType = elementType ?? selector.ReturnType.GetIEnumerableElementType();
            }

            /// <inheritdoc/>
            public IProducer BaseProducer { get; }

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public IExprEnumerable GetNested(ParameterExpression parameter) =>
                selector.InlineArguments(parameter).AsExprEnumerable();
        }

        /// <summary>
        /// An nested enumerable that is produced as the result of performing a SelectMany on a nested enumerable.
        /// </summary>
        internal readonly struct SelectManyNestedExprEnumerable : INestedExprEnumerable
        {
            private readonly INestedExprEnumerable baseEnumerable;
            private readonly LambdaExpression selector;

            /// <summary>
            /// Creates a new instance of <see cref="SelectManyNestedExprEnumerable"/>.
            /// </summary>
            /// <param name="baseEnumerable">The base enumerable being wrapped.</param>
            /// <param name="selector">A function which takes a MoveNext continuation and generates a new MoveNext continuation.</param>
            /// <param name="elementType">The new element type of the producer.</param>
            public SelectManyNestedExprEnumerable(
                INestedExprEnumerable baseEnumerable,
                LambdaExpression selector,
                Type? elementType = null)
            {
                this.baseEnumerable = baseEnumerable;
                this.selector = selector;

                BaseProducer = baseEnumerable.BaseProducer;
                ElementType = elementType ?? selector.ReturnType.GetIEnumerableElementType();
            }

            /// <inheritdoc/>
            public IProducer BaseProducer { get; }

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public IExprEnumerable GetNested(ParameterExpression parameter)
            {
                var nestedEnumerable = baseEnumerable.GetNested(parameter);
                return nestedEnumerable switch
                {
                    LinearExprEnumerable linear => new SelectManyLinearExprEnumerable(linear, selector, ElementType),
                    INestedExprEnumerable nested => new SelectManyNestedExprEnumerable(nested, selector, ElementType),
                    _ => throw new ArgumentException("Must be a linear or nested enumerable."),
                };
            }
        }
    }
}
