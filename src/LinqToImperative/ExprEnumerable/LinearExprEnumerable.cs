using LinqToImperative.Producers;
using System;

namespace LinqToImperative.ExprEnumerable
{
    /// <summary>
    /// Represents an enumerable where there is a single linear sequence of values being produced.
    /// </summary>
    /// <param name="Producer">The producer which defines the values being produced.</param>
    internal record struct LinearExprEnumerable(IProducer Producer) : IExprEnumerable
    {
        /// <inheritdoc/>
        public Type ElementType => Producer.ElementType;
    }
}
