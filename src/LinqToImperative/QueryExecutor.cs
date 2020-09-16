using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToImperative.Internal;

namespace LinqToImperative
{
    /// <summary>
    /// Default implementation of <see cref="IQueryExecutor"/> which will translate IQueryable calls
    /// into efficient imperative code.
    /// </summary>
    public class QueryExecutor : IQueryExecutor
    {
        /// <inheritdoc/>
        public Func<T> Compile<T>(Expression expression)
        {
            var visitor = new QueryTranslationVisitor();
            expression = visitor.Visit(expression);
            var query = Expression.Lambda<Func<T>>(expression);
            return query.Compile();
        }

        /// <inheritdoc/>
        public Func<TParam1, TResult> Compile<TParam1, TResult>(Expression expression, ParameterExpression param1)
        {
            var visitor = new QueryTranslationVisitor();
            expression = visitor.Visit(expression);
            var query = Expression.Lambda<Func<TParam1, TResult>>(expression, param1);
            return query.Compile();
        }

        /// <inheritdoc/>
        public T Execute<T>(Expression expression) => this.Compile<T>(expression).Invoke();

        /// <summary>
        /// An ExpressionVisitor that translates IQueryable calls into <see cref="EnumerableExpression"/> objects.
        /// </summary>
        private class QueryTranslationVisitor : ExpressionVisitor
        {
            /// <inheritdoc/>
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(Queryable))
                {
                    Expression source = this.Visit(node.Arguments[0]);
                    if (source is EnumerableExpression enumerableExpression)
                    {
                        switch (node.Method.Name)
                        {
                            case "Aggregate" when node.Arguments.Count == 3:
                                {
                                    Expression seed = this.Visit(node.Arguments[1]);
                                    LambdaExpression func = Unquote(this.Visit(node.Arguments[2]));
                                    return enumerableExpression.Aggregate(func, seed);
                                }

                            case "Where" when node.Arguments.Count == 2:
                                {
                                    LambdaExpression predicate = Unquote(this.Visit(node.Arguments[1]));
                                    return enumerableExpression.Where(predicate);
                                }

                            case "Select" when node.Arguments.Count == 2:
                                {
                                    LambdaExpression selector = Unquote(this.Visit(node.Arguments[1]));
                                    return enumerableExpression.Select(selector);
                                }

                            case "SelectMany" when node.Arguments.Count == 2:
                                {
                                    LambdaExpression selector = Unquote(this.Visit(node.Arguments[1]));
                                    return enumerableExpression.SelectMany(selector);
                                }
                        }
                    }
                }
                else if (node.Method.DeclaringType == typeof(ImperativeQueryableExtensions))
                {
                    if (node.Method.Name == nameof(ImperativeQueryableExtensions.AsImperativeQueryable))
                    {
                        var firstArg = node.Arguments[0];
                        if (firstArg.Type.IsArray)
                        {
                            return EnumerableExpressionExtensions.OfArray(firstArg);
                        }
                        else
                        {
                            return EnumerableExpressionExtensions.OfEnumerable(firstArg);
                        }
                    }
                }

                return base.VisitMethodCall(node);
            }

            /// <summary>
            /// Takes a quoted or unquoted lambda expression and returns the LambdaExpression object.
            /// </summary>
            /// <param name="expr">The expression to get the lambda from.</param>
            /// <returns>The lambda expression.</returns>
            private static LambdaExpression Unquote(Expression expr)
            {
                return expr switch
                {
                    UnaryExpression unExpr when unExpr.NodeType == ExpressionType.Quote && unExpr.Operand is LambdaExpression lambdaExpr => lambdaExpr,
                    LambdaExpression lambdaExpr => lambdaExpr,
                    _ => throw new ArgumentException("Argument is not a quoted or unquoted lambda expression.", nameof(expr)),
                };
            }
        }
    }
}
