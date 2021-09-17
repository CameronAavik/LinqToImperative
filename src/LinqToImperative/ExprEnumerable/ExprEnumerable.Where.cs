using LinqToImperative.Producers;
using LinqToImperative.Utils;
using System;
using System.Linq.Expressions;

namespace LinqToImperative.ExprEnumerable
{
    /// <summary>
    /// Implementation of LINQ Where for IExprEnumerable.
    /// </summary>
    public static partial class ExprEnumerableExtensions
    {
        /// <summary>
        /// Takes an enumerable of element T, and an expression from T -> bool, and filters the
        /// enumerable to elements where the predicate is true.
        /// </summary>
        /// <param name="enumerable">The enumerable to filter.</param>
        /// <param name="predicate">The predicate to filter on.</param>
        /// <returns>The filtered enumerable.</returns>
        internal static IExprEnumerable Where(this IExprEnumerable enumerable, LambdaExpression predicate)
        {
            return enumerable switch
            {
                LinearExprEnumerable linear => new LinearExprEnumerable(new WhereProducer(linear.Producer, predicate)),
                INestedExprEnumerable nested => new WhereNestedExprEnumerable(nested, predicate),
                _ => throw new ArgumentException("Must be a linear or nested enumerable."),
            };
        }

        /// <summary>
        /// A producer which takes another producer and wraps the MoveNext implementation.
        /// </summary>
        internal readonly struct WhereProducer : IProducer
        {
            private readonly IProducer baseProducer;
            private readonly LambdaExpression predicate;

            /// <summary>
            /// Creates a new instance of <see cref="WhereProducer"/>.
            /// </summary>
            /// <param name="baseProducer">The base producer being wrapped.</param>
            /// <param name="predicate">A predicate to filter on.</param>
            public WhereProducer(IProducer baseProducer, LambdaExpression predicate)
            {
                this.baseProducer = baseProducer;
                this.predicate = predicate;
                ElementType = baseProducer.ElementType;
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
                var predicate = this.predicate;
                return baseProducer.MoveNext(cur => Expression.IfThen(predicate.InlineArguments(cur), continuation(cur)));
            }
        }

        /// <summary>
        /// A nested enumerable which takes another nested enumerable and wraps the MoveNext implementation.
        /// </summary>
        internal readonly struct WhereNestedExprEnumerable : INestedExprEnumerable
        {
            private readonly INestedExprEnumerable baseNestedEnumerable;
            private readonly LambdaExpression selector;

            /// <summary>
            /// Creates a new instance of <see cref="WhereNestedExprEnumerable"/>.
            /// </summary>
            /// <param name="baseNestedEnumerable">The base nested enumerable being wrapped.</param>
            /// <param name="predicate">A predicate to filter on.</param>
            public WhereNestedExprEnumerable(INestedExprEnumerable baseNestedEnumerable, LambdaExpression predicate)
            {
                BaseProducer = baseNestedEnumerable.BaseProducer;
                this.baseNestedEnumerable = baseNestedEnumerable;
                selector = predicate;
                ElementType = predicate.ReturnType;
            }

            /// <inheritdoc/>
            public IProducer BaseProducer { get; }

            /// <inheritdoc/>
            public Type ElementType { get; }

            /// <inheritdoc/>
            public IExprEnumerable GetNested(ParameterExpression parameter)
            {
                return baseNestedEnumerable.GetNested(parameter).Where(selector);
            }
        }
    }
}
