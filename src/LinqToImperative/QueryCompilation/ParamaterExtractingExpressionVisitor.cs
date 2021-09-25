using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using LinqToImperative.Utils;

namespace LinqToImperative.QueryCompilation
{
    internal class ParamaterExtractingExpressionVisitor : ExpressionVisitor
    {
        public ParamaterExtractingExpressionVisitor(QueryContext context)
        {
            Context = context;
        }

        public QueryContext Context { get; private set; }

        protected override Expression VisitMember(MemberExpression memberExpression)
        {
            // If we are accessing a field from a closure, then we should parameterise it
            if (memberExpression.Expression is ConstantExpression expr && expr.Value is not null && IsClosureType(expr.Type))
            {
                var closureMember = new ClosureMember(expr.Value, memberExpression.Member);
                if (Context.TryGetExistingClosureParameter(closureMember, out var parameter))
                    return parameter;

                // If the expression can't be evaluated, don't parameterise it.
                if (!memberExpression.TryEvaluate(out var value))
                    return base.VisitMember(memberExpression);

                // Generate a new parameter and update the cache
                var paramExpression = Expression.Parameter(memberExpression.Type, "p");

                var contextParameter = new ContextParameter(paramExpression, value);
                Context = Context.AddClosureParameter(closureMember, contextParameter);
                return paramExpression;
            }

            return base.VisitMember(memberExpression);
        }

        private static bool IsClosureType(Type type) =>
            type.Attributes.HasFlag(TypeAttributes.NestedPrivate)
                && Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), inherit: true);
    }
}
