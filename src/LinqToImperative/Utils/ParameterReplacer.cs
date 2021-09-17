using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToImperative.Utils
{
    /// <summary>
    /// An expression visitor for replacing parameter expressions wherever they appear inside an expression.
    /// </summary>
    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly IReadOnlyList<ParameterExpression> parameters;
        private readonly IReadOnlyList<Expression> replacements;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterReplacer"/> class.
        /// </summary>
        /// <param name="parameters">Parameters to replace.</param>
        /// <param name="replacements">What to replace the parameters with.</param>
        public ParameterReplacer(IReadOnlyList<ParameterExpression> parameters, IReadOnlyList<Expression> replacements)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            this.replacements = replacements ?? throw new ArgumentNullException(nameof(replacements));
        }

        /// <inheritdoc/>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (node == parameters[i])
                {
                    return replacements[i];
                }
            }

            return base.VisitParameter(node);
        }
    }
}
