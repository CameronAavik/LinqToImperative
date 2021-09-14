using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// A class that defines an Enumerable imperatively using expressions.
    /// </summary>
    public abstract class EnumerableExpression : Expression
    {
        /// <summary>
        /// Gets the type of the elements of the enumerable.
        /// </summary>
        public abstract Type ElementType { get; }

        /// <inheritdoc/>
        public override Type Type => typeof(IQueryable<>).MakeGenericType(ElementType);

        /// <inheritdoc />
        public sealed override ExpressionType NodeType => ExpressionType.Extension;

        /// <inheritdoc/>
        protected override Expression VisitChildren(ExpressionVisitor visitor) => this;
    }
}
