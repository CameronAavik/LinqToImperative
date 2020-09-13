using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// A set of helper methods for using <see cref="Expression"/>s.
    /// </summary>
    public static class ExpressionHelpers
    {
        /// <summary>
        /// Takes a lambda expression and subsitutes the lambda arguments wherever they appear in
        /// the body with a set of substitute parameters.
        /// </summary>
        /// <param name="expr">The lambda expression to substitute.</param>
        /// <param name="subs">The parameters to substitute in the body.</param>
        /// <returns>The body of the lambda after substituting the arguments.</returns>
        public static Expression Substitute(this LambdaExpression expr, params Expression[] subs)
        {
            var subsDict = new Dictionary<ParameterExpression, Expression>();
            for (int i = 0; i < subs.Length; i++)
            {
                subsDict[expr.Parameters[i]] = subs[i];
            }

            var replacer = new ParameterReplacer(subsDict);
            return replacer.Visit(expr.Body);
        }

        /// <summary>
        /// An expression visitor for replacing parameter expressions wherever they appear inside
        /// an expression.
        /// </summary>
        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, Expression> substitutions;

            /// <summary>
            /// Initializes a new instance of the <see cref="ParameterReplacer"/> class.
            /// </summary>
            /// <param name="substitutions">A dictionary containing the replacements.</param>
            public ParameterReplacer(Dictionary<ParameterExpression, Expression> substitutions)
            {
                this.substitutions = substitutions;
            }

            /// <inheritdoc/>
            protected override Expression VisitParameter(ParameterExpression node) =>
                this.substitutions.TryGetValue(node, out Expression? actualValue)
                    ? actualValue
                    : base.VisitParameter(node);
        }
    }
}
