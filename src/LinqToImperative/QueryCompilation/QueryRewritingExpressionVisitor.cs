using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToImperative.Converters;
using LinqToImperative.Expressions;
using LinqToImperative.Utils;
using LinqToImperative.ExprEnumerable;

namespace LinqToImperative.QueryCompilation
{
    /// <summary>
    /// An ExpressionVisitor that translates IQueryable calls into <see cref="EnumerableExpression"/> objects.
    /// </summary>
    internal class QueryRewritingExpressionVisitor : ExpressionVisitor
    {
        private static readonly MethodInfo ImperativeQueryableAggregateMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.Aggregate(0, (_, _) => 0));
        private static readonly MethodInfo ImperativeQueryableAggregateWithSeedExprMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.Aggregate(() => 0, (_, _) => 0));
        private static readonly MethodInfo ImperativeQueryableWhereMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.Where(_ => true));
        private static readonly MethodInfo ImperativeQueryableSelectMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.Select(_ => 0));
        private static readonly MethodInfo ImperativeQueryableSelectManyMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.SelectMany(_ => default(ImperativeQueryable<int>)!));
        private static readonly MethodInfo ImperativeQueryableSelectManyWithProjectionMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.SelectMany(_ => default(ImperativeQueryable<int>)!, (_, _) => 0));

        /// <inheritdoc/>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;

            if (method.DeclaringType == typeof(ImperativeQueryableExtensions))
            {
                Expression source = Visit(node.Arguments[0]);
                if (source is EnumerableExpression enumerableExpression)
                {
                    var genericMethod = method.IsGenericMethod ? method.GetGenericMethodDefinition() : null;
                    switch (method.Name)
                    {
                        case "Aggregate" when genericMethod == ImperativeQueryableAggregateMethod:
                            {
                                Expression seed = Visit(node.Arguments[1]);
                                LambdaExpression func = Visit(node.Arguments[2]).Unquote();
                                return enumerableExpression.Enumerable.Aggregate(func, seed);
                            }

                        case "Aggregate" when genericMethod == ImperativeQueryableAggregateWithSeedExprMethod:
                            {
                                LambdaExpression seed = Visit(node.Arguments[1]).Unquote();
                                LambdaExpression func = Visit(node.Arguments[2]).Unquote();
                                return enumerableExpression.Enumerable.Aggregate(func, seed);
                            }

                        case "Where" when genericMethod == ImperativeQueryableWhereMethod:
                            {
                                LambdaExpression predicate = Visit(node.Arguments[1]).Unquote();
                                var enumerable = enumerableExpression.Enumerable.Where(predicate);
                                return new EnumerableExpression(enumerable);
                            }

                        case "Select" when genericMethod == ImperativeQueryableSelectMethod:
                            {
                                LambdaExpression selector = Visit(node.Arguments[1]).Unquote();
                                var enumerable = enumerableExpression.Enumerable.Select(selector);
                                return new EnumerableExpression(enumerable);
                            }

                        case "SelectMany" when genericMethod == ImperativeQueryableSelectManyMethod:
                            {
                                LambdaExpression selector = Visit(node.Arguments[1]).Unquote();
                                var enumerable = enumerableExpression.Enumerable.SelectMany(selector);
                                return new EnumerableExpression(enumerable);
                            }

                        case "SelectMany" when genericMethod == ImperativeQueryableSelectManyWithProjectionMethod:
                            {
                                LambdaExpression selector = Visit(node.Arguments[1]).Unquote();
                                LambdaExpression projection = Visit(node.Arguments[2]).Unquote();
                                var enumerable = enumerableExpression.Enumerable.SelectMany(selector, projection);
                                return new EnumerableExpression(enumerable);
                            }

                        default:
                            throw new InvalidOperationException("Unsupported LINQ operation");
                    }
                }
            }
            else if (method.Name == nameof(ArrayExtensions.AsImperativeQueryable))
            {
                var enumerable = node.Arguments[0].AsExprEnumerable();
                return new EnumerableExpression(enumerable);
            }

            return base.VisitMethodCall(node);
        }
    }
}
