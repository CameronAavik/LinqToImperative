using LinqToImperative.QueryTree;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToImperative.QueryCompilation
{
    /// <summary>
    /// Interface that defines methods for compiling queries.
    /// </summary>
    internal interface IQueryCompiler
    {
        /// <summary>
        /// Compiles an expression that takes an array of parameters.
        /// </summary>
        /// <typeparam name="TFunc">The type of the function being compiled</typeparam>
        /// <param name="lambdaExpression">The lambda expression to compile.</param>
        /// <returns>The compiled expression as a function.</returns>
        TFunc Compile<TFunc>(Expression<TFunc> lambdaExpression) where TFunc : Delegate;
        T ExecuteQuery<T>(IQuery<T> queryResult, IReadOnlyList<ContextParameter> contextParameters);
    }
}
