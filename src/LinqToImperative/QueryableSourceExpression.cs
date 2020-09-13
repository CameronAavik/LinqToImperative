using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToImperative
{
    /// <summary>
    /// An expression representing the source of an <see cref="ImperativeQueryable{T}"/>.
    /// </summary>
    public class QueryableSourceExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryableSourceExpression"/> class.
        /// </summary>
        /// <param name="source">The queryable source.</param>
        public QueryableSourceExpression(IQueryableSource source)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Type = typeof(IQueryable<>).MakeGenericType(source.ElementType);
        }

        /// <summary>
        /// Gets the source of the <see cref="ImperativeQueryable{T}"/>.
        /// </summary>
        public IQueryableSource Source { get; }

        /// <inheritdoc/>
        public override Type Type { get; }

        /// <inheritdoc/>
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <inheritdoc/>
        protected override Expression VisitChildren(ExpressionVisitor visitor) => this;
    }
}
