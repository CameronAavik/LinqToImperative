using LinqToImperative.Converters;
using LinqToImperative.Producers;
using LinqToImperative.Utils;
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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
        /// <param name="projection">The projection function.</param>
        /// <returns>The flatmapped enumerable.</returns>
        internal static IExprEnumerable SelectMany(this IExprEnumerable enumerable, LambdaExpression selector, LambdaExpression? projection = null)
        {
            var elementType = projection?.ReturnType ?? selector.ReturnType.GetElementType()!;
            return enumerable switch
            {
                LinearExprEnumerable linear => new SelectManyLinearExprEnumerable(linear, selector, projection, elementType),
                INestedExprEnumerable nested => new SelectManyNestedExprEnumerable(nested, selector, projection, elementType),
                _ => throw new ArgumentException("Must be a linear or nested enumerable."),
            };
        }

        /// <summary>
        /// An nested enumerable that is produced as the result of performing a SelectMany on a linear enumerable.
        /// </summary>
        internal readonly struct SelectManyLinearExprEnumerable : INestedExprEnumerable
        {
            private readonly LinearExprEnumerable baseEnumerable;
            private readonly LambdaExpression selector;
            private readonly LambdaExpression? projection;

            /// <summary>
            /// Creates a new instance of <see cref="SelectManyLinearExprEnumerable"/>.
            /// </summary>
            /// <param name="baseEnumerable">The base enumerable being wrapped.</param>
            /// <param name="selector">A function which takes an element and produces an enumerable of elements.</param>
            /// <param name="projection">
            /// A function which takes an element produced by the selector along with the element that produced it and returns a project element.
            /// </param>
            /// <param name="elementType">The new element type of the producer.</param>
            public SelectManyLinearExprEnumerable(
                LinearExprEnumerable baseEnumerable,
                LambdaExpression selector,
                LambdaExpression? projection,
                Type elementType)
            {
                this.baseEnumerable = baseEnumerable;
                this.selector = selector;
                this.projection = projection;
                ElementType = elementType;
            }

            /// <inheritdoc/>
            public IProducer BaseProducer => baseEnumerable.Producer;

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public IExprEnumerable GetNested(ParameterExpression parameter)
            {
                var nested = selector.InlineArguments(parameter).AsExprEnumerable();
                if (projection is null)
                {
                    return nested;
                }
                else
                {
                    var replacer = new ParameterReplacer(
                    new[] { projection.Parameters[0] },
                    new[] { parameter });

                    var inlinedProjection = Expression.Lambda(
                        replacer.Visit(projection.Body),
                        new ReadOnlyCollectionBuilder<ParameterExpression>(1) { projection.Parameters[1] });

                    return nested.Select(inlinedProjection);
                }
            }

            /// <inheritdoc/>
            public IExprEnumerable VisitChildren(ExpressionVisitor visitor)
            {
                var newBaseEnumerable = (LinearExprEnumerable)baseEnumerable.VisitChildren(visitor);
                var newSelector = (LambdaExpression)visitor.Visit(selector);
                var newProjection = projection == null ? null : (LambdaExpression)visitor.Visit(projection);
                return newBaseEnumerable == baseEnumerable && newSelector == selector && newProjection == projection
                    ? this
                    : new SelectManyLinearExprEnumerable(newBaseEnumerable, newSelector, newProjection, ElementType);
            }
        }

        /// <summary>
        /// An nested enumerable that is produced as the result of performing a SelectMany on a nested enumerable.
        /// </summary>
        internal readonly struct SelectManyNestedExprEnumerable : INestedExprEnumerable
        {
            private readonly INestedExprEnumerable baseEnumerable;
            private readonly LambdaExpression selector;
            private readonly LambdaExpression? projection;

            /// <summary>
            /// Creates a new instance of <see cref="SelectManyNestedExprEnumerable"/>.
            /// </summary>
            /// <param name="baseEnumerable">The base enumerable being wrapped.</param>
            /// <param name="selector">A function which takes a MoveNext continuation and generates a new MoveNext continuation.</param>
            /// <param name="projection">
            /// A function which takes an element produced by the selector along with the element that produced it and returns a project element.
            /// </param>
            /// <param name="elementType">The new element type of the producer.</param>
            public SelectManyNestedExprEnumerable(
                INestedExprEnumerable baseEnumerable,
                LambdaExpression selector,
                LambdaExpression? projection,
                Type elementType)
            {
                this.baseEnumerable = baseEnumerable;
                this.selector = selector;
                this.projection = projection;
                BaseProducer = baseEnumerable.BaseProducer;
                ElementType = elementType;
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
                    LinearExprEnumerable linear => new SelectManyLinearExprEnumerable(linear, selector, projection, ElementType),
                    INestedExprEnumerable nested => new SelectManyNestedExprEnumerable(nested, selector, projection, ElementType),
                    _ => throw new ArgumentException("Must be a linear or nested enumerable."),
                };
            }

            /// <inheritdoc/>
            public IExprEnumerable VisitChildren(ExpressionVisitor visitor)
            {
                var newBaseEnumerable = (INestedExprEnumerable)baseEnumerable.VisitChildren(visitor);
                var newSelector = (LambdaExpression)visitor.Visit(selector);
                var newProjection = projection == null ? null : (LambdaExpression)visitor.Visit(projection);
                return newBaseEnumerable == baseEnumerable && newSelector == selector && newProjection == projection
                    ? this
                    : new SelectManyNestedExprEnumerable(newBaseEnumerable, newSelector, newProjection, ElementType);
            }
        }
    }
}
