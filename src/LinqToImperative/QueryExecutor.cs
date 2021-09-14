using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.CompilerServices;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using LinqToImperative.Internal;

namespace LinqToImperative
{
    /// <summary>
    /// Default implementation of <see cref="IQueryExecutor"/> which will translate IQueryable calls
    /// into efficient imperative code.
    /// </summary>
    public class QueryExecutor : IQueryExecutor
    {
        private static readonly ConcurrentDictionary<LambdaExpression, Delegate> Cache = new(new ExpressionEqualityComparer());

        /// <inheritdoc/>
        public Func<T> Compile<T>(Expression expression)
        {
            var query = Expression.Lambda<Func<T>>(expression);
            return (Func<T>)CompileCore(query);
        }

        /// <inheritdoc/>
        public Func<TParam1, TResult> Compile<TParam1, TResult>(Expression expression, ParameterExpression param1)
        {
            var query = Expression.Lambda<Func<TParam1, TResult>>(expression, param1);
            return (Func<TParam1, TResult>)CompileCore(query);
        }

        private Delegate CompileCore(LambdaExpression expr)
        {
            var visitor = new QueryTranslationVisitor();
            var translatedExpr = visitor.Visit(expr);

            var n = visitor.Parameters.Count + visitor.EnumerableSourceParameters.Count;
            if (n == 0)
            {
                return Cache.GetOrAdd((LambdaExpression)translatedExpr, l => l.Compile());
            }
            else
            {
                var parameterArr = new ParameterExpression[n];
                var values = new object?[n];

                var i = 0;
                foreach ((var param, var value) in visitor.Parameters.Values)
                {
                    parameterArr[i] = param;
                    values[i] = value;
                    i++;
                }

                foreach ((var source, var param) in visitor.EnumerableSourceParameters)
                {
                    parameterArr[i] = param;
                    values[i] = source.Expression.Evaluate();
                    i++;
                }

                var parameterisedLambda = Expression.Lambda(translatedExpr, parameterArr);

                var f = Cache.GetOrAdd(parameterisedLambda, l => l.Compile());
                return (Delegate)f.DynamicInvoke(values)!;
            }
        }

        /// <inheritdoc/>
        public T Execute<T>(Expression expression) => Compile<T>(expression).Invoke();

        /// <summary>
        /// An ExpressionVisitor that translates IQueryable calls into <see cref="EnumerableExpression"/> objects.
        /// </summary>
        public class QueryTranslationVisitor : ExpressionVisitor
        {
            private static readonly MethodInfo AggregateMethod = GetQueryableMethod(x => x.Aggregate(0, (_, _) => 0));
            private static readonly MethodInfo WhereMethod = GetQueryableMethod(x => x.Where(_ => true));
            private static readonly MethodInfo SelectMethod = GetQueryableMethod(x => x.Select(_ => 0));
            private static readonly MethodInfo SelectManyMethod = GetQueryableMethod(x => x.SelectMany(_ => new int[0]));

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

                if (method.DeclaringType == typeof(Queryable))
                {
                    Expression source = Visit(node.Arguments[0]);
                    if (source is EnumerableExpression enumerableExpression)
                    {
                        var genericMethod = method.IsGenericMethod ? method.GetGenericMethodDefinition() : null;
                        switch (method.Name)
                        {
                            case "Aggregate" when genericMethod == AggregateMethod:
                            {
                                Expression seed = Visit(node.Arguments[1]);
                                LambdaExpression func = Visit(node.Arguments[2]).Unquote();
                                return Visit(enumerableExpression.Aggregate(func, seed));
                            }

                            case "Where" when genericMethod == WhereMethod:
                            {
                                LambdaExpression predicate = Visit(node.Arguments[1]).Unquote();
                                return Visit(enumerableExpression.Where(predicate));
                            }

                            case "Select" when genericMethod == SelectMethod:
                            {
                                LambdaExpression selector = Visit(node.Arguments[1]).Unquote();
                                return Visit(enumerableExpression.Select(selector));
                            }

                            case "SelectMany" when genericMethod == SelectManyMethod:
                            {
                                LambdaExpression selector = Visit(node.Arguments[1]).Unquote();
                                return Visit(enumerableExpression.SelectMany(selector));
                            }

                            default:
                                throw new InvalidOperationException("Unsupported LINQ operation");
                        }
                    }
                }
                else if (method.DeclaringType == typeof(ImperativeQueryableExtensions))
                {
                    if (method.Name == nameof(ImperativeQueryableExtensions.AsImperativeQueryable))
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

            /// <inheritdoc/>
            protected override Expression VisitMember(MemberExpression memberExpression)
            {
                // if we are accessing a field from a closure, then we should parameterise it
                if (memberExpression.Expression is ConstantExpression expr && expr.Value is not null && IsClosureType(expr.Type))
                {
                    if (!Parameters.TryGetValue((expr.Value, memberExpression.Member), out var parameter))
                    {
                        var value = memberExpression.Member switch
                        {
                            FieldInfo fieldInfo => fieldInfo.GetValue(expr.Value),
                            PropertyInfo propInfo => propInfo.GetValue(expr.Value),
                            _ => expr.Evaluate()
                        };

                        parameter = (Expression.Parameter(memberExpression.Type, "p"), value);
                        Parameters[(expr.Value, memberExpression.Member)] = parameter;
                    }

                    return parameter.Param;
                }

                return base.VisitMember(memberExpression);
            }

            /// <inheritdoc/>
            protected override Expression VisitExtension(Expression node)
            {
                node = base.VisitExtension(node);

                if (node is EnumerableSourceExpression expr)
                {
                    if (expr.Expression is ParameterExpression paramExpr)
                    {
                        return paramExpr;
                    }

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

            private static MethodInfo GetQueryableMethod<T>(Expression<Func<IQueryable<int>, T>> f) =>
                GetMethod(f);

            private static MethodInfo GetMethod<T1, T2>(Expression<Func<T1, T2>> f) =>
                ((MethodCallExpression)f.Body).Method.GetGenericMethodDefinition();
        }
    }
}
