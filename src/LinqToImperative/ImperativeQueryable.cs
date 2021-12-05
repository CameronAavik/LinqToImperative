using LinqToImperative.QueryTree;
using System.Collections;
using System.Collections.Generic;

namespace LinqToImperative
{
    /// <summary>
    /// A public wrapper for the underlying expression-backed enumerable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ImperativeQueryable<T> : IEnumerable<T>
    {
        internal ImperativeQueryable(StreamQuery<T> querySource, QueryContext context)
        {
            QuerySource = querySource;
            Context = context;
        }

        internal StreamQuery<T> QuerySource { get; }

        internal QueryContext Context { get; }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => throw new System.NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
