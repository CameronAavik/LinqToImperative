using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using LinqToImperative.Converters;
using LinqToImperative.Expressions;
using LinqToImperative.Utils;
using LinqToImperative.ExprEnumerable;

namespace LinqToImperative.QueryCompilation
{
    /// <summary>
    /// An ExpressionVisitor that translates IQueryable calls into <see cref="EnumerableExpression"/> objects.
    /// </summary>
    internal class QueryTranslationVisitor : ExpressionVisitor
    {
        private static readonly MethodInfo ImperativeQueryableAggregateMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.Aggregate(0, (_, _) => 0));
        private static readonly MethodInfo ImperativeQueryableWhereMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.Where(_ => true));
        private static readonly MethodInfo ImperativeQueryableSelectMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.Select(_ => 0));
        private static readonly MethodInfo ImperativeQueryableSelectManyMethod = ReflectionUtils.GetGenericMethod<ImperativeQueryable<int>>(x => x.SelectMany(_ => new int[0]));
        private static readonly MethodInfo QueryableAggregateMethod = ReflectionUtils.GetGenericMethod<IQueryable<int>>(x => x.Aggregate(0, (_, _) => 0));
        private static readonly MethodInfo QueryableWhereMethod = ReflectionUtils.GetGenericMethod<IQueryable<int>>(x => x.Where(_ => true));
        private static readonly MethodInfo QueryableSelectMethod = ReflectionUtils.GetGenericMethod<IQueryable<int>>(x => x.Select(_ => 0));
        private static readonly MethodInfo QueryableSelectManyMethod = ReflectionUtils.GetGenericMethod<IQueryable<int>>(x => x.SelectMany(_ => new int[0]));

        /// <summary>
        /// Gets the enumerable source parameters
        /// </summary>
        public readonly Dictionary<EnumerableSourceExpression, ParameterExpression> EnumerableSourceParameters = new();

        /// <summary>
        /// Gets the extracted expressions
        /// </summary>
        public readonly Dictionary<(object Closure, MemberInfo Member), (ParameterExpression Param, object? Value)> Parameters = new();

        /// <inheritdoc/>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var method = node.Method;

            if (method.DeclaringType == typeof(Queryable) || method.DeclaringType == typeof(ImperativeQueryableExtensions))
            {
                Expression source = Visit(node.Arguments[0]);
                if (source is EnumerableExpression enumerableExpression)
                {
                    var genericMethod = method.IsGenericMethod ? method.GetGenericMethodDefinition() : null;
                    switch (method.Name)
                    {
                        case nameof(Queryable.Aggregate) when genericMethod == QueryableAggregateMethod || genericMethod == ImperativeQueryableAggregateMethod:
                            {
                                Expression seed = Visit(node.Arguments[1]);
                                LambdaExpression func = Visit(node.Arguments[2]).Unquote();
                                return Visit(enumerableExpression.Enumerable.Aggregate(func, seed));
                            }

                        case nameof(Queryable.Where) when genericMethod == QueryableWhereMethod || genericMethod == ImperativeQueryableWhereMethod:
                            {
                                LambdaExpression predicate = Visit(node.Arguments[1]).Unquote();
                                var enumerable = enumerableExpression.Enumerable.Where(predicate);
                                return Visit(new EnumerableExpression(enumerable));
                            }

                        case nameof(Queryable.Select) when genericMethod == QueryableSelectMethod || genericMethod == ImperativeQueryableSelectMethod:
                            {
                                LambdaExpression selector = Visit(node.Arguments[1]).Unquote();
                                var enumerable = enumerableExpression.Enumerable.Select(selector);
                                return Visit(new EnumerableExpression(enumerable));
                            }

                        case nameof(Queryable.SelectMany) when genericMethod == QueryableSelectManyMethod || genericMethod == ImperativeQueryableSelectManyMethod:
                            {
                                LambdaExpression selector = Visit(node.Arguments[1]).Unquote();
                                var enumerable = enumerableExpression.Enumerable.SelectMany(selector);
                                return Visit(new EnumerableExpression(enumerable));
                            }

                        default:
                            throw new InvalidOperationException("Unsupported LINQ operation");
                    }
                }
            }
            else if (method.Name == nameof(ArrayExtensions.AsImperativeQueryable))
            {
                var enumerable = node.Arguments[0].AsExprEnumerable();
                return Visit(new EnumerableExpression(enumerable));
            }

            return base.VisitMethodCall(node);
        }

        /// <inheritdoc/>
        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            // If we are accessing a field from a closure, then we should parameterise it
            if (memberExpression.Expression is ConstantExpression expr && expr.Value is not null && IsClosureType(expr.Type))
            {
                // If the field has already been visited, reuse the parameter
                if (Parameters.TryGetValue((expr.Value, memberExpression.Member), out var parameter))
                    return parameter.Param;

                // If the expression can't be evaluated, don't parameterise it.
                if (!memberExpression.TryEvaluate(out var value))
                    return base.VisitMember(memberExpression);

                // Generate a new parameter and update the cache
                var paramExpression = Expression.Parameter(memberExpression.Type, "p");
                Parameters[(expr.Value, memberExpression.Member)] = (paramExpression, value);

                return paramExpression;
            }

            return base.VisitMember(memberExpression);
        }

        /// <inheritdoc/>
        protected override Expression VisitExtension(Expression node)
        {
            node = base.VisitExtension(node);

            if (node is EnumerableSourceExpression expr)
            {
                if (!EnumerableSourceParameters.TryGetValue(expr, out var cachedParamExpr))
                {
                    cachedParamExpr = Expression.Parameter(expr.Type, "source");
                    EnumerableSourceParameters[expr] = cachedParamExpr;
                }

                return cachedParamExpr;
            }

            return node;
        }

        private static bool IsClosureType(Type type) =>
            type.Attributes.HasFlag(TypeAttributes.NestedPrivate)
                && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), inherit: true);
    }
}
