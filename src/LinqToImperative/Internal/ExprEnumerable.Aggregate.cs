using System;
using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// Class containing the implementation of Aggregate for <see cref="IExprEnumerable"/> objects.
    /// </summary>
    public static partial class ExprEnumerableExtensions
    {
        /// <summary>
        /// Takes two expressions, func (S -> T -> S) and seed (S), and an enumerable with elements
        /// of type T, and aggregates (aka folds) the elements to produce a valid of type S.
        /// </summary>
        /// <param name="enumerable">The enumerable to aggregate.</param>
        /// <param name="func">The aggregator function.</param>
        /// <param name="seed">The initial state used when aggregating.</param>
        /// <returns>An expression representing the calculation of the aggregation.</returns>
        internal static Expression Aggregate(this IExprEnumerable enumerable, LambdaExpression func, Expression seed)
        {
            ParameterExpression accVar = Expression.Variable(seed.Type, "acc");
            return Expression.Block(
                new[] { accVar },
                Expression.Assign(accVar, seed),
                enumerable.AggregateRaw(e => Expression.Assign(accVar, func.Substitute(accVar, e))),
                accVar);
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
            switch (enumerable)
            {
                case ILinearExprEnumerable linear:
                    {
                        /*
                         * while (true)
                         * {
                         *     if (<enumerable.hasNext>)
                         *         <enumerable.moveNext(consumer)>;
                         *     else
                         *         break;
                         * }
                         */

                        LabelTarget label = Expression.Label();
                        Expression loopExpr =
                            Expression.Loop(
                                Expression.IfThenElse(
                                    linear.HasNext,
                                    linear.MoveNext(func),
                                    Expression.Break(label)),
                                label);

                        return linear.Initialize(loopExpr);
                    }

                case INestedExprEnumerable nested:
                    return nested.BaseEnumerable.AggregateRaw(e => nested.GetNestedEnumerable(e).AggregateRaw(func));
                default:
                    throw new Exception("ExprEnumerable is in an invalid state, must be a linear or nested enumerable.");
            }
        }
    }
}
