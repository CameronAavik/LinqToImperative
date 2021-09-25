using LinqToImperative.QueryTree;
using LinqToImperative.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqToImperative.QueryCompilation
{
    /// <summary>
    /// Class responsible for compiling imperative queryables.
    /// </summary>
    internal class QueryCompiler : IQueryCompiler
    {
        private static readonly ConcurrentDictionary<CompiledQueryCacheKey, Delegate> Cache = new();

        public static readonly QueryCompiler Instance = new();

        /// <inheritdoc/>
        public TDelegate Compile<TDelegate>(Expression<TDelegate> expr) where TDelegate : Delegate
        {
            var visitor = new QueryRewritingExpressionVisitor();
            var translatedExpr = (Expression<TDelegate>)visitor.Visit(expr);
            return translatedExpr.Compile();
        }

        public T ExecuteQuery<T>(IQuery<T> queryResult, IReadOnlyList<ContextParameter> contextParameters)
        {
            var cacheKey = new CompiledQueryCacheKey(queryResult, contextParameters);
            var func = Cache.GetOrAdd(cacheKey, k => Compile(k.Query, k.Parameters));

            var n = contextParameters.Count;
            switch (n)
            {
                case 0:
                    return ((Func<T>)func)();
                case 1:
                    return ((Func<object?, T>)func)(contextParameters[0].Value);
                case 2:
                    return ((Func<object?, object?, T>)func)(contextParameters[0].Value, contextParameters[1].Value);
                case 3:
                    return ((Func<object?, object?, object?, T>)func)(contextParameters[0].Value, contextParameters[1].Value, contextParameters[2].Value);
                default:
                    {
                        // We use ReadOnlyCollectionBuilder to prevent Expression.Lambda from allocating and copying the parameters
                        var values = new object?[n];

                        for (int i = 0; i < contextParameters.Count; i++)
                            values[i] = contextParameters[i].Value;

                        return ((Func<object?[], T>)func)(values);
                    }
            }
        }

        private static Delegate Compile(IQuery query, IReadOnlyList<ContextParameter> parameters)
        {
            var visitor = new QueryRewritingExpressionVisitor();
            var queryExpressionTree = query.VisitQuery(visitor);

            var paramsCount = parameters.Count;
            LambdaExpression lambdaToCompile;
            if (paramsCount == 0)
            {
                lambdaToCompile = Expression.Lambda(queryExpressionTree, null);
            }
            else if (paramsCount < 4)
            {
                var lambdaParams = new ReadOnlyCollectionBuilder<ParameterExpression>(paramsCount);
                var bodyVariables = new ReadOnlyCollectionBuilder<ParameterExpression>(parameters.Count);
                var bodyExpressions = new ReadOnlyCollectionBuilder<Expression>(parameters.Count + 1);

                foreach (var contextParameter in parameters)
                {
                    var lambdaParam = Expression.Parameter(typeof(object), "param");
                    lambdaParams.Add(lambdaParam);

                    var parameter = contextParameter.Parameter;
                    bodyVariables.Add(parameter);
                    bodyExpressions.Add(Expression.Assign(parameter, Expression.Convert(lambdaParam, parameter.Type)));
                }

                bodyExpressions.Add(queryExpressionTree);
                lambdaToCompile = Expression.Lambda(Expression.Block(bodyVariables, bodyExpressions), lambdaParams);
            }
            else
            {
                var paramsArrayParam = Expression.Parameter(typeof(object?[]), "params");

                var variables = new ReadOnlyCollectionBuilder<ParameterExpression>(parameters.Count);
                var expressions = new ReadOnlyCollectionBuilder<Expression>(parameters.Count + 1);

                for (int i = 0; i < parameters.Count; i++)
                {
                    var parameter = parameters[i].Parameter;
                    variables.Add(parameter);
                    expressions.Add(
                        Expression.Assign(
                            parameter,
                            Expression.Convert(
                                Expression.ArrayIndex(
                                    paramsArrayParam,
                                    Expression.Constant(i)),
                                parameter.Type)));
                }

                expressions.Add(queryExpressionTree);

                lambdaToCompile = Expression.Lambda(
                    Expression.Block(variables, expressions),
                    new ReadOnlyCollectionBuilder<ParameterExpression>(1) { paramsArrayParam });
            }

            return lambdaToCompile.Compile();
        }

        internal sealed class CompiledQueryCacheKey
        {
            public CompiledQueryCacheKey(IQuery query, IReadOnlyList<ContextParameter> parameters)
            {
                Query = query;
                Parameters = parameters;
            }

            public IQuery Query { get; }
            public IReadOnlyList<ContextParameter> Parameters { get; }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(this, obj))
                    return true;

                if (obj is not CompiledQueryCacheKey otherKey)
                    return false;

                if (Parameters.Count != otherKey.Parameters.Count)
                    return false;

                var comparer = new ExpressionTreeComparer();
                for (int i = 0; i < Parameters.Count; i++)
                    comparer.AddParameter(Parameters[i].Parameter, otherKey.Parameters[i].Parameter);
                
                return comparer.CompareAndValidateLabels(Query, otherKey.Query);
            }

            public override int GetHashCode()
            {
                int hash = Query.GetHashCode();
                for (int i = 0; i < Parameters.Count; i++)
                {
                    ContextParameter parameter = Parameters[i];
                    hash = HashHelpers.CombineHash(hash, parameter.Parameter.Type.GetHashCode());
                }

                return hash;
            }
        }
    }
}
