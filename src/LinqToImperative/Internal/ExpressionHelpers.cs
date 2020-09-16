using System.Linq;
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
            var replacer = new ParameterReplacer(expr.Parameters.ToArray(), subs);
            return replacer.Visit(expr.Body);
        }

        /// <summary>
        /// An expression visitor for replacing parameter expressions wherever they appear inside
        /// an expression.
        /// </summary>
        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression[] pars;
            private readonly Expression[] replacements;

            /// <summary>
            /// Initializes a new instance of the <see cref="ParameterReplacer"/> class.
            /// </summary>
            /// <param name="pars">Parameters to replace.</param>
            /// <param name="replacements">What to replace the parameters with.</param>
            public ParameterReplacer(ParameterExpression[] pars, Expression[] replacements)
            {
                this.pars = pars ?? throw new System.ArgumentNullException(nameof(pars));
                this.replacements = replacements ?? throw new System.ArgumentNullException(nameof(replacements));
            }

            /// <inheritdoc/>
            protected override Expression VisitParameter(ParameterExpression node)
            {
                for (int i = 0; i < this.pars.Length; i++)
                {
                    if (node == this.pars[i])
                    {
                        return this.replacements[i];
                    }
                }

                return base.VisitParameter(node);
            }
        }
    }
}
