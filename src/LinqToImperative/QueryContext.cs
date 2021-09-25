using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToImperative
{
    internal record QueryContext(
        ImmutableList<ContextParameter> Parameters,
        ImmutableList<(ClosureMember Closure, ParameterExpression Parameter)> ClosureParameterLookup)
    {
        internal static QueryContext Empty = new(
            ImmutableList<ContextParameter>.Empty,
            ImmutableList<(ClosureMember, ParameterExpression)>.Empty);

        internal static QueryContext WithRootParameter(ContextParameter parameter) => new(
            ImmutableList.Create(parameter),
            ImmutableList<(ClosureMember, ParameterExpression)>.Empty);

        internal QueryContext AddClosureParameter(ClosureMember closure, ContextParameter contextParameter) =>
            new(Parameters.Add(contextParameter), ClosureParameterLookup.Add((closure, contextParameter.Parameter)));

        internal bool TryGetExistingClosureParameter(
            ClosureMember closureInfo,
            [NotNullWhen(returnValue: true)]out ParameterExpression? closureParameter)
        {
            foreach ((var key, var parameter) in ClosureParameterLookup)
            {
                if (closureInfo.Member == key.Member && closureInfo.Closure == closureInfo.Closure)
                {
                    closureParameter = parameter;
                    return true;
                }
            }

            closureParameter = null;
            return false;
        }
    }

    internal record struct ContextParameter(ParameterExpression Parameter, object? Value);

    internal record struct ClosureMember(object Closure, MemberInfo Member);
}
