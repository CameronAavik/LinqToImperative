using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

            var paramList = new List<ParameterExpression>();
            var paramAssignList = new List<Expression>();

            foreach ((ParameterExpression param, Expression? expr) in visitor.Params)
            {
                if (expr is not null)
                {
                    paramList.Add(param);
                    paramAssignList.Add(Expression.Assign(param, expr));
                }
            }

            paramAssignList.Add(expression);

            expression = Expression.Block(paramList, paramAssignList);

            var query = Expression.Lambda<Func<T>>(expression);
            return query.Compile();
        }

        /// <inheritdoc/>
        public T Execute<T>(Expression expression) => this.Compile<T>(expression).Invoke();

        /// <summary>
        /// An ExpressionVisitor that translates IQueryable calls into <see cref="IExprEnumerable"/> objects.
        /// </summary>
        private class QueryTranslationVisitor : ExpressionVisitor
        {
            /// <summary>
            /// Gets a dictionary for keeping track of all external params that need to be assigned.
            /// </summary>
            public Dictionary<ParameterExpression, Expression?> Params { get; } = new Dictionary<ParameterExpression, Expression?>();

            /// <inheritdoc/>
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(Queryable))
                {
                    Expression source = this.Visit(node.Arguments[0]);
                    if (source is ConstantExpression constExpr && constExpr.Value is IExprEnumerable exprEnumerable)
                    {
                        switch (node.Method.Name)
                        {
                            case "Aggregate" when node.Arguments.Count == 3:
                                {
                                    Expression seed = this.Visit(node.Arguments[1]);
                                    LambdaExpression func = Unquote(this.Visit(node.Arguments[2]));
                                    return exprEnumerable.Aggregate(func, seed);
                                }

                            case "Where" when node.Arguments.Count == 2:
                                {
                                    LambdaExpression predicate = Unquote(this.Visit(node.Arguments[1]));
                                    return Expression.Constant(exprEnumerable.Where(predicate));
                                }

                            case "Select" when node.Arguments.Count == 2:
                                {
                                    LambdaExpression selector = Unquote(this.Visit(node.Arguments[1]));
                                    return Expression.Constant(exprEnumerable.Select(selector));
                                }

                            case "SelectMany" when node.Arguments.Count == 2:
                                {
                                    LambdaExpression selector = Unquote(this.Visit(node.Arguments[1]));
                                    return Expression.Constant(exprEnumerable.SelectMany(selector));
                                }
                        }
                    }
                }

                return base.VisitMethodCall(node);
            }

            /// <inheritdoc/>
            protected override Expression VisitExtension(Expression node)
            {
                if (node is QueryableSourceExpression expr)
                {
                    foreach ((ParameterExpression param, Expression? val) in expr.Source.QuerySourceParams)
                    {
                        this.Params[param] = val;
                    }

                    return Expression.Constant(expr.Source.GetExprEnumerable());
                }

                return base.VisitExtension(node);
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
