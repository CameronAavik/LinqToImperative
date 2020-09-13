using System;
using System.Linq.Expressions;

namespace LinqToImperative
{
    /// <summary>
    /// Interface that defines methods for compiling and executing a query.
    /// </summary>
    public interface IQueryExecutor
    {
        /// <summary>
        /// Compiles an expression.
        /// </summary>
        /// <typeparam name="T">The type returned by the expression.</typeparam>
        /// <param name="expression">The expression to compile.</param>
        /// <returns>The compiled expression as a function.</returns>
        Func<T> Compile<T>(Expression expression);

        /// <summary>
        /// Compiles and executes the expression.
        /// </summary>
        /// <typeparam name="T">The type returned by the expression.</typeparam>
        /// <param name="expression">The expression to execute.</param>
        /// <returns>The result after executing the expression.</returns>
        T Execute<T>(Expression expression);
    }
}
