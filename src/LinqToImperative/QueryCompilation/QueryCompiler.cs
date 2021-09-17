using LinqToImperative.Utils.Nuqleon;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqToImperative.QueryCompilation
{
    /// <summary>
    /// Class responsible for compiling imperative queryables.
    /// </summary>
    internal class QueryCompiler : IQueryCompiler
    {
        private static readonly ConcurrentDictionary<LambdaExpression, Delegate> Cache = new(new ExpressionEqualityComparer());

        /// <summary>
        /// Singleton instance of the Query Compiler
        /// </summary>
        public static QueryCompiler Instance = new();

        /// <inheritdoc/>
        public TFunc Compile<TFunc>(Expression<TFunc> expr) where TFunc : Delegate
        {
            var visitor = new QueryTranslationVisitor();
            var translatedExpr = visitor.Visit(expr);

            var n = visitor.Parameters.Count + visitor.EnumerableSourceParameters.Count;

            if (n == 0)
            {
                return (TFunc)Cache.GetOrAdd((LambdaExpression)translatedExpr, l => l.Compile());
            }
            else
            {
                // We use ReadOnlyCollectionBuilder to prevent Expression.Lambda from allocating and copying the parameters
                var parameterCollection = new ReadOnlyCollectionBuilder<ParameterExpression>(n);
                var values = new object?[n];

                var i = 0;
                foreach ((var param, var value) in visitor.Parameters.Values)
                {
                    parameterCollection.Add(param);
                    values[i++] = value;
                }

                foreach ((var source, var param) in visitor.EnumerableSourceParameters)
                {
                    parameterCollection.Add(param);
                    values[i++] = source.Source;
                }

                var parameterisedLambda = Expression.Lambda(translatedExpr, parameterCollection);

                var f = Cache.GetOrAdd(parameterisedLambda, l => l.Compile());
                return (TFunc)f.DynamicInvoke(values)!;
            }
        }
    }
}
