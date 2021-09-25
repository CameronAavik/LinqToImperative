using LinqToImperative.QueryCompilation;
using LinqToImperative.QueryTree;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToImperative
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Extensions for ImperativeQueryable
    /// </summary>
    public static class ImperativeQueryableExtensions
    {
        public static TRet Aggregate<T, TRet>(this ImperativeQueryable<T> source, TRet seed, Expression<Func<TRet, T, TRet>> func)
        {
            var context = source.Context;
            func = ExtractParameters(func, ref context);
            var queryResult = new QueryTypes.Aggregate<T, TRet>(source.QuerySource, seed, func);
            return CompileAndExecute(queryResult, context);
        }

        public static TRet Aggregate<T, TRet>(this ImperativeQueryable<T> source, Expression<Func<TRet>> seed, Expression<Func<TRet, T, TRet>> func)
        {
            var context = source.Context;
            seed = ExtractParameters(seed, ref context);
            func = ExtractParameters(func, ref context);
            var queryResult = new QueryTypes.AggregateWithSeedExpr<T, TRet>(source.QuerySource, seed, func);
            return CompileAndExecute(queryResult, context);
        }

        public static ImperativeQueryable<T2> SelectMany<T1, T2>(this ImperativeQueryable<T1> source, Expression<Func<T1, IEnumerable<T2>>> selector)
        {
            var context = source.Context;
            selector = ExtractParameters(selector, ref context);
            var streamQuery = new QueryTypes.SelectMany<T1, T2>(source.QuerySource, selector);
            return new(streamQuery, context);
        }

        public static ImperativeQueryable<T3> SelectMany<T1, T2, T3>(
            this ImperativeQueryable<T1> source,
            Expression<Func<T1, IEnumerable<T2>>> collectionSelector,
            Expression<Func<T1, T2, T3>> resultSelector)
        {
            var context = source.Context;
            collectionSelector = ExtractParameters(collectionSelector, ref context);
            resultSelector = ExtractParameters(resultSelector, ref context);
            var streamQuery = new QueryTypes.SelectMany<T1, T2, T3>(source.QuerySource, collectionSelector, resultSelector);
            return new(streamQuery, context);
        }

        public static ImperativeQueryable<T2> Select<T1, T2>(this ImperativeQueryable<T1> source, Expression<Func<T1, T2>> selector)
        {
            var context = source.Context;
            selector = ExtractParameters(selector, ref context);
            var streamQuery = new QueryTypes.Select<T1, T2>(source.QuerySource, selector);
            return new(streamQuery, context);
        }

        public static ImperativeQueryable<T> Where<T>(this ImperativeQueryable<T> source, Expression<Func<T, bool>> predicate)
        {
            var context = source.Context;
            predicate = ExtractParameters(predicate, ref context);
            var streamQuery = new QueryTypes.Where<T>(source.QuerySource, predicate);
            return new(streamQuery, context);
        }

        private static T CompileAndExecute<T>(IQuery<T> queryResult, QueryContext queryContext) =>
            QueryCompiler.Instance.ExecuteQuery(queryResult, queryContext.Parameters);

        private static Expression<TDelegate> ExtractParameters<TDelegate>(Expression<TDelegate> func, ref QueryContext context) where TDelegate : Delegate
        {
            var visitor = new ParamaterExtractingExpressionVisitor(context);
            func = (Expression<TDelegate>)visitor.Visit(func);
            context = visitor.Context;
            return func;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
