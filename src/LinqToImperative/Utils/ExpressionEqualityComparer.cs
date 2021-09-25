using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using static LinqToImperative.Utils.HashHelpers;

namespace LinqToImperative.Utils
{
    /// <summary>
    /// Expression equality comparer for expression trees. Default behavior matches trees in a structural fashion.
    /// </summary>
    public class ExpressionEqualityComparer : IEqualityComparer<Expression>
    {
        /// <summary>
        /// Singleton instance of the <see cref="ExpressionEqualityComparer"/>
        /// </summary>
        public static readonly ExpressionEqualityComparer Instance = new();

        /// <inheritdoc/>
        public bool Equals(Expression? x, Expression? y) => x == y || new ExpressionTreeComparer().CompareAndValidateLabels(x, y);

        /// <inheritdoc/>
        public int GetHashCode(Expression obj)
        {
            if (obj == null)
                return 0;

            int hash = (int)obj.NodeType;

            switch (obj.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.Power:
                case ExpressionType.RightShift:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.AndAssign:
                case ExpressionType.Assign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.OrAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                    var binaryExpression = (BinaryExpression)obj;
                    hash = CombineHash(hash, GetHashCode(binaryExpression.Left));
                    hash = CombineHash(hash, GetHashCode(binaryExpression.Right));
                    hash = AddExpressionToHashIfNotNull(hash, binaryExpression.Conversion);
                    return AddToHashIfNotNull(hash, binaryExpression.Method);

                case ExpressionType.Block:
                    var blockExpression = (BlockExpression)obj;
                    hash = AddExpressionListToHash(hash, blockExpression.Variables);
                    return AddExpressionListToHash(hash, blockExpression.Expressions);

                case ExpressionType.Conditional:
                    var conditionalExpression = (ConditionalExpression)obj;
                    return CombineHash(hash, GetHashCode(conditionalExpression.Test), GetHashCode(conditionalExpression.IfTrue), GetHashCode(conditionalExpression.IfFalse));

                case ExpressionType.Constant:
                    return CombineHash(hash, ((ConstantExpression)obj).Value?.GetHashCode() ?? 0);

                case ExpressionType.DebugInfo:
                    var debugInfoExpression = (DebugInfoExpression)obj;
                    hash = CombineHash(hash, debugInfoExpression.Document.GetHashCode());
                    hash = CombineHash(hash, debugInfoExpression.StartLine);
                    hash = CombineHash(hash, debugInfoExpression.StartColumn);
                    hash = CombineHash(hash, debugInfoExpression.EndLine);
                    hash = CombineHash(hash, debugInfoExpression.EndColumn);
                    return CombineHash(hash, debugInfoExpression.IsClear.GetHashCode());

                case ExpressionType.Default:
                    return CombineHash(hash, ((DefaultExpression)obj).Type.GetHashCode());

                case ExpressionType.Dynamic:
                    var dynamicExpression = (DynamicExpression)obj;
                    hash = CombineHash(hash, dynamicExpression.DelegateType.GetHashCode(), dynamicExpression.Binder.GetHashCode());
                    return AddExpressionListToHash(hash, dynamicExpression.Arguments);

                case ExpressionType.Goto:
                    var gotoExpression = (GotoExpression)obj;
                    hash = AddExpressionToHashIfNotNull(hash, gotoExpression.Value);
                    return CombineHash(hash, (int)gotoExpression.Kind);

                case ExpressionType.Index:
                    var indexExpression = (IndexExpression)obj;
                    hash = AddExpressionToHashIfNotNull(hash, indexExpression.Object);
                    hash = AddExpressionListToHash(hash, indexExpression.Arguments);
                    return AddToHashIfNotNull(hash, indexExpression.Indexer);

                case ExpressionType.Invoke:
                    var invocationExpression = (InvocationExpression)obj;
                    hash = CombineHash(hash, GetHashCode(invocationExpression.Expression));
                    return AddExpressionListToHash(hash, invocationExpression.Arguments);

                case ExpressionType.Label:
                    return AddExpressionToHashIfNotNull(hash, ((LabelExpression)obj).DefaultValue);

                case ExpressionType.Loop:
                    var loopExpression = (LoopExpression)obj;
                    return CombineHash(hash, GetHashCode(loopExpression.Body), loopExpression.BreakLabel == null ? 0 : 1, loopExpression.ContinueLabel == null ? 0 : 1);

                case ExpressionType.Lambda:
                    var lambdaExpression = (LambdaExpression)obj;
                    hash = CombineHash(hash, GetHashCode(lambdaExpression.Body));
                    return AddExpressionListToHash(hash, lambdaExpression.Parameters);

                case ExpressionType.ListInit:
                    var listInitExpression = (ListInitExpression)obj;
                    hash = CombineHash(hash, GetHashCode(listInitExpression.NewExpression));
                    return AddInitializersToHash(hash, listInitExpression.Initializers);

                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)obj;
                    hash = AddExpressionToHashIfNotNull(hash, memberExpression.Expression);
                    return CombineHash(hash, memberExpression.Member.GetHashCode());

                case ExpressionType.MemberInit:
                    var memberInitExpression = (MemberInitExpression)obj;
                    hash = CombineHash(hash, GetHashCode(memberInitExpression.NewExpression));
                    return AddMemberBindingsToHash(hash, memberInitExpression.Bindings);

                case ExpressionType.Call:
                    var methodCallExpression = (MethodCallExpression)obj;
                    hash = AddExpressionToHashIfNotNull(hash, methodCallExpression.Object);
                    hash = AddExpressionListToHash(hash, methodCallExpression.Arguments);
                    return CombineHash(hash, methodCallExpression.Method.GetHashCode());

                case ExpressionType.New:
                    var newExpression = (NewExpression)obj;
                    hash = AddExpressionListToHash(hash, newExpression.Arguments);
                    hash = AddToHashIfNotNull(hash, newExpression.Constructor);
                    return AddListToHashIfNotNull(hash, newExpression.Members);

                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
                    var newArrayExpression = (NewArrayExpression)obj;
                    return AddExpressionListToHash(hash, newArrayExpression.Expressions);

                case ExpressionType.Parameter:
                    var parameterExpression = (ParameterExpression)obj;
                    return CombineHash(hash, parameterExpression.Type.GetHashCode());

                case ExpressionType.RuntimeVariables:
                    return AddExpressionListToHash(hash, ((RuntimeVariablesExpression)obj).Variables);

                case ExpressionType.Switch:
                    var switchExpression = (SwitchExpression)obj;
                    hash = CombineHash(hash, GetHashCode(switchExpression.SwitchValue));
                    hash = AddExpressionToHashIfNotNull(hash, switchExpression.DefaultBody);
                    hash = AddToHashIfNotNull(hash, switchExpression.Comparison);
                    foreach (var switchCase in switchExpression.Cases)
                    {
                        hash = CombineHash(hash, GetHashCode(switchCase.Body));
                        hash = AddExpressionListToHash(hash, switchCase.TestValues);
                    }

                    return hash;

                case ExpressionType.Try:
                    var tryExpression = (TryExpression)obj;
                    hash = CombineHash(hash, GetHashCode(tryExpression.Body));
                    hash = AddExpressionToHashIfNotNull(hash, tryExpression.Fault);
                    hash = AddExpressionToHashIfNotNull(hash, tryExpression.Finally);
                    foreach (var handler in tryExpression.Handlers)
                    {
                        hash = CombineHash(hash, GetHashCode(handler.Body), handler.Test.GetHashCode());
                        hash = AddExpressionToHashIfNotNull(hash, handler.Variable);
                        hash = AddExpressionToHashIfNotNull(hash, handler.Filter);
                    }

                    return hash;

                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    var typeBinaryExpression = (TypeBinaryExpression)obj;
                    return CombineHash(hash, GetHashCode(typeBinaryExpression.Expression), typeBinaryExpression.TypeOperand.GetHashCode());

                case ExpressionType.ArrayLength:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus:
                case ExpressionType.Decrement:
                case ExpressionType.Increment:
                case ExpressionType.IsFalse:
                case ExpressionType.IsTrue:
                case ExpressionType.OnesComplement:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.Throw:
                case ExpressionType.Unbox:
                    var unaryExpression = (UnaryExpression)obj;
                    hash = CombineHash(hash, GetHashCode(unaryExpression.Operand));
                    return AddToHashIfNotNull(hash, unaryExpression.Method);

                case ExpressionType.Extension:
                    return CombineHash(hash, obj.GetHashCode());

                default:
                    throw new NotImplementedException($"GetHashCode is not implemented for node type {obj.GetType().Name}");
            }

            int AddToHashIfNotNull<T>(int hash, T? t)
            {
                return t != null ? CombineHash(hash, t.GetHashCode()) : hash;
            }

            int AddExpressionToHashIfNotNull(int hash, Expression? t)
            {
                return t != null ? CombineHash(hash, GetHashCode(t)) : hash;
            }

            int AddExpressionListToHash<T>(int hash, IReadOnlyList<T> expressions)
                where T : Expression
            {
                for (int i = 0; i < expressions.Count; i++)
                {
                    T v = expressions[i];
                    hash = CombineHash(hash, GetHashCode(v));
                }

                return hash;
            }

            int AddListToHashIfNotNull<T>(int hash, IReadOnlyList<T>? items)
            {
                if (items is not null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        T v = items[i];
                        hash = CombineHash(hash, v?.GetHashCode() ?? 0);
                    }
                }

                return hash;
            }

            int AddInitializersToHash(int hash, IReadOnlyList<ElementInit> initializers)
            {
                for (int i = 0; i < initializers.Count; i++)
                {
                    ElementInit v = initializers[i];
                    hash = AddExpressionListToHash(hash, v.Arguments);
                    hash = CombineHash(hash, v.AddMethod.GetHashCode());
                }

                return hash;
            }

            int AddMemberBindingsToHash(int hash, IReadOnlyList<MemberBinding> memberBindings)
            {
                for (int i = 0; i < memberBindings.Count; i++)
                {
                    MemberBinding? memberBinding = memberBindings[i];
                    hash = CombineHash(hash, memberBinding.Member.GetHashCode(), memberBinding.BindingType.GetHashCode());

                    switch (memberBinding)
                    {
                        case MemberAssignment memberAssignment:
                            hash = CombineHash(hash, GetHashCode(memberAssignment.Expression));
                            break;

                        case MemberListBinding memberListBinding:
                            hash = AddInitializersToHash(hash, memberListBinding.Initializers);
                            break;

                        case MemberMemberBinding memberMemberBinding:
                            hash = AddMemberBindingsToHash(hash, memberMemberBinding.Bindings);
                            break;
                    }
                }

                return hash;
            }
        }
    }
}
