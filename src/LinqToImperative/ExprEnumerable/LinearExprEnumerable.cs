using LinqToImperative.Producers;
using System;
using System.Linq.Expressions;

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

        /// <inheritdoc/>
        public IExprEnumerable VisitChildren(ExpressionVisitor visitor)
        {
            var newProducer = Producer.VisitChildren(visitor);
            return newProducer == Producer ? this : new LinearExprEnumerable(newProducer);
        }
    }
}
