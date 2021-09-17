using System;
using System.Collections;
using System.Linq.Expressions;

namespace LinqToImperative.Expressions
{
    /// <summary>
    /// Expression depicting a source enumerable which should be lifted out into a parameter.
    /// </summary>
    internal class EnumerableSourceExpression : Expression
    {
        /// <summary>
        /// Creates a new EnumerableSourceExpression
        /// </summary>
        /// <param name="source"></param>
        public EnumerableSourceExpression(IEnumerable source)
        {
            Source = source;
        }

        /// <summary>
        /// The enumerable source
        /// </summary>
        public IEnumerable Source { get; }

        /// <inheritdoc/>
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <inheritdoc/>
        public override Type Type => Source.GetType();

        /// <inheritdoc/>
        public override bool CanReduce => false;

        /// <inheritdoc/>
        protected override Expression VisitChildren(ExpressionVisitor visitor) => this;
    }
}
