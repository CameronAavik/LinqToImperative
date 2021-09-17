using LinqToImperative.Converters;
using LinqToImperative.Producers;
using LinqToImperative.Utils;
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqToImperative.ExprEnumerable
{
    /// <summary>
    /// Implementation of LINQ Aggregate for IExprEnumerable.
    /// </summary>
    public static partial class ExprEnumerableExtensions
    {
        /// <summary>
        /// Takes two expressions, func (S -> T -> S) and seed (S), and an enumerable with elements
        /// of type T, and aggregates (aka folds) the elements to produce a valid of type S.
        /// </summary>
        /// <param name="expression">The enumerable to aggregate.</param>
        /// <param name="func">The aggregator function.</param>
        /// <param name="seed">The initial state used when aggregating.</param>
        /// <returns>An expression representing the calculation of the aggregation.</returns>
        internal static Expression Aggregate(this IExprEnumerable expression, LambdaExpression func, Expression seed)
        {
            ParameterExpression accVar = Expression.Variable(seed.Type, "acc");

            return Expression.Block(
                new ReadOnlyCollectionBuilder<ParameterExpression>(1) { accVar },
                new ReadOnlyCollectionBuilder<Expression>(3)
                {
                    Expression.Assign(accVar, seed),
                    expression.AggregateRaw(e => Expression.Assign(accVar, func.InlineArguments(accVar, e))),
                    accVar
                });
        }

        /// <summary>
        /// Takes an aggregator function and uses it to aggregate the elements of the enumerable.
        /// The aggregator function takes a parameter of type T and returns a void expression.
        /// This aggregator function can be thought of like the body of a for loop.
        /// </summary>
        /// <param name="enumerable">The enumerable to aggregate.</param>
        /// <param name="func">The aggregator function.</param>
        /// <returns>An expression that contains the calculation of the aggregation.</returns>
        private static Expression AggregateRaw(this IExprEnumerable enumerable, Func<ParameterExpression, Expression> func)
        {
            Expression GenLinearAggregate(IProducer producer)
            {
                LabelTarget label = Expression.Label();

                Expression loopExpr =
                    Expression.Loop(
                        Expression.IfThenElse(
                            producer.HasNext,
                            producer.MoveNext(func),
                            Expression.Break(label)),
                        label);

                return producer.Initialize(loopExpr);
            }

            return enumerable switch
            {
                LinearExprEnumerable linear => GenLinearAggregate(linear.Producer),
                INestedExprEnumerable nested => nested.BaseProducer
                    .AsExprEnumerable()
                    .AggregateRaw(e => nested.GetNested(e).AggregateRaw(func)),
                _ => throw new ArgumentException("Must be a linear or nested enumerable.", nameof(enumerable)),
            };
        }
    }
}
