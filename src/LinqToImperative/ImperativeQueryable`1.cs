using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToImperative.Internal;

namespace LinqToImperative
{
    /// <summary>
    /// The implementation of <see cref="IOrderedQueryable{T}"/> for capturing the expression tree to be
    /// translated to imperative code.
    /// </summary>
    /// <typeparam name="T">The type of the elements returned when the query is executed.</typeparam>
    public class ImperativeQueryable<T> : IOrderedQueryable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImperativeQueryable{T}"/> class.
        /// </summary>
        /// <param name="queryProvider">The query provider.</param>
        /// <param name="expression">The underlying expression tree.</param>
        public ImperativeQueryable(IQueryProvider queryProvider, Expression expression)
        {
            this.Provider = queryProvider ?? throw new ArgumentNullException(nameof(queryProvider));
            this.Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        /// <inheritdoc/>
        public Type ElementType => typeof(T);

        /// <inheritdoc/>
        public IQueryProvider Provider { get; }

        /// <inheritdoc/>
        public Expression Expression { get; }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
            => this.Provider.Execute<IEnumerable<T>>(this.Expression).GetEnumerator()!;

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => this.Provider.Execute<IEnumerable>(this.Expression).GetEnumerator();
    }
}
