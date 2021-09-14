using System;
using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// Expression depicting a source enumerable which should be lifted out into a parameter.
    /// </summary>
    public class EnumerableSourceExpression : Expression
    {
        /// <summary>
        /// Creates a new EnumerableSourceExpression
        /// </summary>
        /// <param name="Expression"></param>
        public EnumerableSourceExpression(Expression Expression)
        {
            this.Expression = Expression;
        }

        /// <summary>
        /// The enumerable source
        /// </summary>
        public Expression Expression { get; }

        /// <inheritdoc/>
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <inheritdoc/>
        public override Type Type => Expression.Type;

        /// <inheritdoc/>
        public override bool CanReduce => Expression.CanReduce;

        /// <inheritdoc/>
        public override Expression Reduce() => Expression.Reduce();

        /// <inheritdoc/>
        protected override Expression VisitChildren(ExpressionVisitor visitor)
        {
            var newExpression = visitor.Visit(Expression);
            return newExpression == Expression ? this : new(newExpression);
        }
    }
}
