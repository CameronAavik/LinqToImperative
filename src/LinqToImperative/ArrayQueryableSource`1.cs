using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using LinqToImperative.Internal;

namespace LinqToImperative
{
    /// <summary>
    /// An implementation of <see cref="IQueryableSource"/> for arrays.
    /// </summary>
    /// <typeparam name="T">The type of the element in the array.</typeparam>
    public class ArrayQueryableSource<T> : IQueryableSource
    {
        private readonly ParameterExpression arrVar;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayQueryableSource{T}"/> class.
        /// </summary>
        /// <param name="arrValue">The source array.</param>
        public ArrayQueryableSource(T[] arrValue)
        {
            this.arrVar = Expression.Variable(typeof(T[]), "arr");
            this.QuerySourceParams = new Dictionary<ParameterExpression, Expression?>()
            {
                [this.arrVar] = Expression.Constant(arrValue, typeof(T[])),
            };
        }

        /// <inheritdoc/>
        public Type ElementType => typeof(T);

        /// <inheritdoc/>
        public Type QuerySourceType => typeof(T[]);

        /// <inheritdoc/>
        public Dictionary<ParameterExpression, Expression?> QuerySourceParams { get; }

        /// <inheritdoc/>
        public IExprEnumerable GetExprEnumerable() => new ArrayExprEnumerable(this.arrVar);
    }
}
