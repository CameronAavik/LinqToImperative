using System;
using System.Linq.Expressions;
using LinqToImperative.QueryCompilation;

namespace LinqToImperative
{
    /// <summary>
    /// A class for compiling linq queries using imperative
    /// </summary>
    public static class LinqToImperative
    {
        /// <summary>
        /// Creates a compiled function that when invoked will execute the query.
        /// </summary>
        /// <typeparam name="TResult">The type of the query result.</typeparam>
        /// <param name="query">The query to compile.</param>
        /// <returns>The compiled query as a function.</returns>
        public static Func<TResult> CompileQuery<TResult>(Expression<Func<TResult>> query) => 
            QueryCompiler.Instance.Compile(query);

        /// <summary>
        /// Creates a compiled function that when invoked will execute the query.
        /// </summary>
        /// <typeparam name="TParam1">The type of the first parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query result.</typeparam>
        /// <param name="query">The query to compile.</param>
        /// <returns>The compiled query as a function.</returns>
        public static Func<TParam1, TResult> CompileQuery<TParam1, TResult>(Expression<Func<TParam1, TResult>> query) => 
            QueryCompiler.Instance.Compile(query);

        /// <summary>
        /// Creates a compiled function that when invoked will execute the query.
        /// </summary>
        /// <typeparam name="TParam1">The type of the first parameter.</typeparam>
        /// <typeparam name="TParam2">The type of the second parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query result.</typeparam>
        /// <param name="query">The query to compile.</param>
        /// <returns>The compiled query as a function.</returns>
        public static Func<TParam1, TParam2, TResult> CompileQuery<TParam1, TParam2, TResult>(Expression<Func<TParam1, TParam2, TResult>> query) => 
            QueryCompiler.Instance.Compile(query);

        /// <summary>
        /// Creates a compiled function that when invoked will execute the query.
        /// </summary>
        /// <typeparam name="TParam1">The type of the first parameter.</typeparam>
        /// <typeparam name="TParam2">The type of the second parameter.</typeparam>
        /// <typeparam name="TParam3">The type of the third parameter.</typeparam>
        /// <typeparam name="TResult">The type of the query result.</typeparam>
        /// <param name="query">The query to compile.</param>
        /// <returns>The compiled query as a function.</returns>
        public static Func<TParam1, TParam2, TParam3, TResult> CompileQuery<TParam1, TParam2, TParam3, TResult>(Expression<Func<TParam1, TParam2, TParam3, TResult>> query) =>
            QueryCompiler.Instance.Compile(query);
    }
}
