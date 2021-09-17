using LinqToImperative.ExprEnumerable;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToImperative.Expressions
{
    /// <summary>
    /// An expression that wraps an expression-backed enumerable.
    /// </summary>
    internal class EnumerableExpression : Expression
    {
        /// <summary>
        /// Creates an instance of <see cref="EnumerableExpression"/>.
        /// </summary>
        /// <param name="enumerable"></param>
        public EnumerableExpression(IExprEnumerable enumerable)
        {
            Enumerable = enumerable;
        }

        /// <summary>
        /// The enumerable defined using expression trees.
        /// </summary>
        public IExprEnumerable Enumerable { get; }

        /// <summary>
        /// The type of the element produced by the enumerable.
        /// </summary>
        public Type ElementType => Enumerable.ElementType;

        /// <inheritdoc/>
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <inheritdoc/>
        public override Type Type => typeof(IQueryable<>).MakeGenericType(ElementType);

        /// <inheritdoc/>
        public override bool CanReduce => false;

        /// <inheritdoc/>
        protected override Expression VisitChildren(ExpressionVisitor visitor) => this;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is EnumerableExpression expr && ReferenceEquals(Enumerable, expr.Enumerable);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Enumerable);
    }
}
