using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToImperative.Utils
{
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Takes a lambda expression and subsitutes the lambda arguments wherever they appear in
        /// the body with a set of substitute parameters.
        /// </summary>
        /// <param name="expr">The lambda expression to substitute.</param>
        /// <param name="subs">The parameters to substitute in the body.</param>
        /// <returns>The body of the lambda after substituting the arguments.</returns>
        public static Expression InlineArguments(this LambdaExpression expr, params Expression[] subs)
        {
            var replacer = new ParameterReplacer(expr.Parameters, subs);
            return replacer.Visit(expr.Body);
        }

        /// <summary>
        /// Removes any Quote expressions that are wrapping a LambdaExpression
        /// </summary>
        /// <param name="expression">Expression to strip quotes from.</param>
        /// <returns>Lambda expression after unquoting. An exception occurs if the specified expression was not a (quoted) LambdaExpression.</returns>
        public static LambdaExpression Unquote(this Expression expression) => expression switch
        {
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: var quotedExpr } => quotedExpr.Unquote(),
            LambdaExpression lambdaExpr => lambdaExpr,
            _ => throw new InvalidOperationException("Expression is not a quoted lambda"),
        };

        public static bool TryEvaluate(this Expression expression, out object? value)
        {
            static bool TryGetMemberValue(object? instance, MemberInfo memberInfo, out object? value)
            {
                try
                {
                    switch (memberInfo)
                    {
                        case FieldInfo fieldInfo:
                            value = fieldInfo.GetValue(instance);
                            return true;
                        case PropertyInfo propInfo:
                            value = propInfo.GetValue(instance);
                            return true;
                    }
                }
                catch
                {
                    // Member access failed, so fall through to default
                }

                value = null;
                return false;
            }

            switch (expression)
            {
                case ConstantExpression constExpr:
                    value = constExpr.Value;
                    return true;

                case MemberExpression { Member: var memberInfo, Expression: null }:
                    return TryGetMemberValue(null, memberInfo, out value);

                case MemberExpression { Member: var memberInfo, Expression: var expr }:
                    if (expr.TryEvaluate(out var exprValue))
                        return TryGetMemberValue(exprValue, memberInfo, out value);

                    // if the expression can't be validated, then return false
                    value = null;
                    return false;

                default:
                    try
                    {
                        value = ((Func<object?>)Expression.Lambda(expression, null).Compile(preferInterpretation: true))();
                        return true;
                    }
                    catch
                    {
                        // Handle case when evaluation fails
                        value = null;
                        return false;
                    }
            }
        }
    }
}
