using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToImperative.Internal;

namespace LinqToImperative
{
    /// <summary>
    /// An interface capture the source data structure for an <see cref="ImperativeQueryable{T}"/>.
    /// </summary>
    public interface IQueryableSource
    {
        /// <summary>
        /// Gets the type of the element in <see cref="QuerySourceType"/>.
        /// </summary>
        Type ElementType { get; }

        /// <summary>
        /// Gets the type of the enumerable being used as the source for the <see cref="ImperativeQueryable{T}"/>.
        /// </summary>
        Type QuerySourceType { get; }

        /// <summary>
        /// Gets a dictionary which contains any parameters that are used to represent the source data.
        /// </summary>
        Dictionary<ParameterExpression, Expression?> QuerySourceParams { get; }

        /// <summary>
        /// Gets an object which contains expressions representing efficient iteration over the source data.
        /// </summary>
        /// <returns>The expression-based enumerable.</returns>
        IExprEnumerable GetExprEnumerable();
    }
}
