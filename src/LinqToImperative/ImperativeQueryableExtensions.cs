using LinqToImperative.ExprEnumerable;
using LinqToImperative.Expressions;
using LinqToImperative.QueryCompilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToImperative
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Extensions for ImperativeQueryable
    /// </summary>
    public static class ImperativeQueryableExtensions
    {
        public static TRet Aggregate<T, TRet>(this ImperativeQueryable<T> source, TRet seed, Expression<Func<TRet, T, TRet>> func) =>
            source.GetEnumerable().Aggregate(func, Expression.Constant(seed)).CompileAndExecute<TRet>();

        public static ImperativeQueryable<T2> SelectMany<T1, T2>(this ImperativeQueryable<T1> source, Expression<Func<T1, IEnumerable<T2>>> selector) =>
            source.GetEnumerable().SelectMany(selector).ToImperativeQueryable<T2>();

        public static ImperativeQueryable<T2> Select<T1, T2>(this ImperativeQueryable<T1> source, Expression<Func<T1, T2>> selector) =>
            source.GetEnumerable().Select(selector).ToImperativeQueryable<T2>();

        public static ImperativeQueryable<T> Where<T>(this ImperativeQueryable<T> source, Expression<Func<T, bool>> predicate) =>
            source.GetEnumerable().Where(predicate).ToImperativeQueryable<T>();

        private static IExprEnumerable GetEnumerable<T>(this ImperativeQueryable<T> source) =>
            ((EnumerableExpression)source.Expression).Enumerable;
        
        private static T CompileAndExecute<T>(this Expression expression) =>
            QueryCompiler.Instance.Compile(Expression.Lambda<Func<T>>(expression, null)).Invoke();

        private static ImperativeQueryable<T> ToImperativeQueryable<T>(this IExprEnumerable enumerable) =>
            ImperativeQueryable<T>.Create(enumerable);
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
