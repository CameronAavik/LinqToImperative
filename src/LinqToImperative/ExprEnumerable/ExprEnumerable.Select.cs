using LinqToImperative.Converters;
using LinqToImperative.Producers;
using LinqToImperative.Utils;
using System;
using System.Linq.Expressions;

namespace LinqToImperative.ExprEnumerable
{
    /// <summary>
    /// Implementation of LINQ Select for IExprEnumerable.
    /// </summary>
    public static partial class ExprEnumerableExtensions
    {
        /// <summary>
        /// Takes a lambda expression that maps the elements of the enumerable and returns an
        /// enumerable of the elements after they were mapped.
        /// </summary>
        /// <param name="enumerable">The enumerable being mapped.</param>
        /// <param name="selector">The mapping function.</param>
        /// <returns>The enumerable after mapping.</returns>
        internal static IExprEnumerable Select(this IExprEnumerable enumerable, LambdaExpression selector)
        {
            return enumerable switch
            {
                LinearExprEnumerable linear => new SelectProducer(linear.Producer, selector).AsExprEnumerable(),
                INestedExprEnumerable nested => new SelectNestedExprEnumerable(nested, selector),
                _ => throw new ArgumentException("Must be a linear or nested enumerable."),
            };
        }

        /// <summary>
        /// A producer which takes another producer and wraps the MoveNext implementation.
        /// </summary>
        internal readonly struct SelectProducer : IProducer
        {
            private readonly IProducer baseProducer;
            private readonly LambdaExpression selector;

            /// <summary>
            /// Creates a new instance of <see cref="SelectProducer"/>.
            /// </summary>
            /// <param name="baseProducer">The base producer being wrapped.</param>
            /// <param name="selector">The mapping function.</param>
            public SelectProducer(IProducer baseProducer, LambdaExpression selector)
            {
                this.baseProducer = baseProducer;
                this.selector = selector;
                ElementType = selector.ReturnType;
            }

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public Expression HasNext => baseProducer.HasNext;

            /// <inheritdoc/>
            public Expression Initialize(Expression continuation) => baseProducer.Initialize(continuation);

            /// <inheritdoc/>
            public Expression MoveNext(Func<ParameterExpression, Expression> continuation)
            {
                var selector = this.selector;

                Expression Inner(ParameterExpression curElem)
                {
                    ParameterExpression newElemVar = Expression.Variable(selector.ReturnType, "newElem");
                    return Expression.Block(
                        new[] { newElemVar },
                        Expression.Assign(newElemVar, selector.InlineArguments(curElem)),
                        continuation(newElemVar));
                }

                return baseProducer.MoveNext(Inner);
            }

            /// <inheritdoc/>
            public IProducer VisitChildren(ExpressionVisitor visitor)
            {
                var newBaseProducer = baseProducer.VisitChildren(visitor);
                var newSelector = (LambdaExpression)visitor.Visit(selector);
                return newBaseProducer == baseProducer && newSelector == selector ? this : new SelectProducer(newBaseProducer, newSelector);
            }
        }

        /// <summary>
        /// A nested enumerable which takes another nested enumerable and wraps the MoveNext implementation.
        /// </summary>
        internal readonly struct SelectNestedExprEnumerable : INestedExprEnumerable
        {
            private readonly INestedExprEnumerable baseNestedEnumerable;
            private readonly LambdaExpression selector;

            /// <summary>
            /// Creates a new instance of <see cref="SelectNestedExprEnumerable"/>.
            /// </summary>
            /// <param name="baseNestedEnumerable">The base nested enumerable being wrapped.</param>
            /// <param name="selector">The mapping function.</param>
            public SelectNestedExprEnumerable(INestedExprEnumerable baseNestedEnumerable, LambdaExpression selector)
            {
                BaseProducer = baseNestedEnumerable.BaseProducer;
                this.baseNestedEnumerable = baseNestedEnumerable;
                this.selector = selector;
                ElementType = selector.ReturnType;
            }

            /// <inheritdoc/>
            public IProducer BaseProducer { get; }

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public IExprEnumerable GetNested(ParameterExpression parameter)
            {
                return baseNestedEnumerable.GetNested(parameter).Select(selector);
            }

            /// <inheritdoc/>
            public IExprEnumerable VisitChildren(ExpressionVisitor visitor)
            {
                var newNestedEnumerable = (INestedExprEnumerable)baseNestedEnumerable.VisitChildren(visitor);
                var newSelector = (LambdaExpression)visitor.Visit(selector);
                return newNestedEnumerable == baseNestedEnumerable && newSelector == selector
                    ? this
                    : new SelectNestedExprEnumerable(newNestedEnumerable, newSelector);
            }
        }
    }
}
