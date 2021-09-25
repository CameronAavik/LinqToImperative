using LinqToImperative.QueryTree;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToImperative.Utils
{
    internal struct ExpressionTreeComparer
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> scope = new();
        private IList<(LabelTarget left, LabelTarget right)>? gotos = null;
        private IDictionary<LabelTarget, LabelTarget>? labels = null;

        public void AddParameter(ParameterExpression left, ParameterExpression right)
        {
            if (!scope.TryAdd(left, right))
                throw new InvalidOperationException("Tried to add parameter that is already in scope");
        }

        public bool CompareAndValidateLabels(IQuery left, IQuery right) => left.CompareWith(right, this) && ValidateLabels();

        public bool CompareAndValidateLabels(Expression? left, Expression? right) => Compare(left, right) && ValidateLabels();

        public bool Compare(Expression? left, Expression? right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            if (left.NodeType != right.NodeType)
                return false;

            if (left.Type != right.Type)
                return false;

            return left.NodeType switch
            {
                ExpressionType.Add
                or ExpressionType.AddChecked
                or ExpressionType.And
                or ExpressionType.AndAlso
                or ExpressionType.ArrayIndex
                or ExpressionType.Coalesce
                or ExpressionType.Divide
                or ExpressionType.Equal
                or ExpressionType.ExclusiveOr
                or ExpressionType.GreaterThan
                or ExpressionType.GreaterThanOrEqual
                or ExpressionType.LeftShift
                or ExpressionType.LessThan
                or ExpressionType.LessThanOrEqual
                or ExpressionType.Modulo
                or ExpressionType.Multiply
                or ExpressionType.MultiplyChecked
                or ExpressionType.NotEqual
                or ExpressionType.Or
                or ExpressionType.OrElse
                or ExpressionType.Power
                or ExpressionType.RightShift
                or ExpressionType.Subtract
                or ExpressionType.SubtractChecked
                or ExpressionType.AddAssign
                or ExpressionType.AddAssignChecked
                or ExpressionType.AndAssign
                or ExpressionType.Assign
                or ExpressionType.DivideAssign
                or ExpressionType.ExclusiveOrAssign
                or ExpressionType.LeftShiftAssign
                or ExpressionType.ModuloAssign
                or ExpressionType.MultiplyAssign
                or ExpressionType.MultiplyAssignChecked
                or ExpressionType.OrAssign
                or ExpressionType.PowerAssign
                or ExpressionType.RightShiftAssign
                or ExpressionType.SubtractAssign
                or ExpressionType.SubtractAssignChecked => CompareBinary((BinaryExpression)left, (BinaryExpression)right),
                ExpressionType.Block => CompareBlock((BlockExpression)left, (BlockExpression)right),
                ExpressionType.Conditional => CompareConditional((ConditionalExpression)left, (ConditionalExpression)right),
                ExpressionType.Constant => CompareConstant((ConstantExpression)left, (ConstantExpression)right),
                ExpressionType.DebugInfo => CompareDebugInfo((DebugInfoExpression)left, (DebugInfoExpression)right),
                ExpressionType.Default => true,
                ExpressionType.Dynamic => CompareDynamic((DynamicExpression)left, (DynamicExpression)right),
                ExpressionType.Goto => CompareGoto((GotoExpression)left, (GotoExpression)right),
                ExpressionType.Index => CompareIndex((IndexExpression)left, (IndexExpression)right),
                ExpressionType.Invoke => CompareInvocation((InvocationExpression)left, (InvocationExpression)right),
                ExpressionType.Label => CompareLabel((LabelExpression)left, (LabelExpression)right),
                ExpressionType.Loop => CompareLoop((LoopExpression)left, (LoopExpression)right),
                ExpressionType.Lambda => CompareLambda((LambdaExpression)left, (LambdaExpression)right),
                ExpressionType.ListInit => CompareListInit((ListInitExpression)left, (ListInitExpression)right),
                ExpressionType.MemberAccess => CompareMember((MemberExpression)left, (MemberExpression)right),
                ExpressionType.MemberInit => CompareMemberInit((MemberInitExpression)left, (MemberInitExpression)right),
                ExpressionType.Call => CompareMethodCall((MethodCallExpression)left, (MethodCallExpression)right),
                ExpressionType.New => CompareNew((NewExpression)left, (NewExpression)right),
                ExpressionType.NewArrayBounds
                or ExpressionType.NewArrayInit => CompareNewArray((NewArrayExpression)left, (NewArrayExpression)right),
                ExpressionType.Parameter => CompareParameter((ParameterExpression)left, (ParameterExpression)right),
                ExpressionType.RuntimeVariables => CompareRuntimeVariables((RuntimeVariablesExpression)left, (RuntimeVariablesExpression)right),
                ExpressionType.Switch => CompareSwitch((SwitchExpression)left, (SwitchExpression)right),
                ExpressionType.Try => CompareTry((TryExpression)left, (TryExpression)right),
                ExpressionType.TypeIs
                or ExpressionType.TypeEqual => CompareTypeBinary((TypeBinaryExpression)left, (TypeBinaryExpression)right),
                ExpressionType.ArrayLength
                or ExpressionType.Convert
                or ExpressionType.ConvertChecked
                or ExpressionType.Negate
                or ExpressionType.NegateChecked
                or ExpressionType.Not
                or ExpressionType.Quote
                or ExpressionType.TypeAs
                or ExpressionType.UnaryPlus
                or ExpressionType.Decrement
                or ExpressionType.Increment
                or ExpressionType.IsFalse
                or ExpressionType.IsTrue
                or ExpressionType.OnesComplement
                or ExpressionType.PostDecrementAssign
                or ExpressionType.PostIncrementAssign
                or ExpressionType.PreDecrementAssign
                or ExpressionType.PreIncrementAssign
                or ExpressionType.Throw
                or ExpressionType.Unbox => CompareUnary((UnaryExpression)left, (UnaryExpression)right),
                ExpressionType.Extension => left == right,
                _ => throw new NotImplementedException($"Equals is not implemented for node type {left.NodeType}"),
            };
        }

        private bool CompareBinary(BinaryExpression a, BinaryExpression b) =>
            Equals(a.Method, b.Method)
                && Compare(a.Left, b.Left)
                && Compare(a.Right, b.Right)
                && Compare(a.Conversion, b.Conversion);

        private bool CompareBlock(BlockExpression a, BlockExpression b)
        {
            if (a.Variables.Count != b.Variables.Count)
                return false;

            EnterParameters(a.Variables, b.Variables);

            try
            {
                return CompareExpressionList(a.Expressions, b.Expressions);
            }
            finally
            {
                ExitParameters(a.Variables);
            }
        }

        private bool CompareConditional(ConditionalExpression a, ConditionalExpression b) =>
            Compare(a.Test, b.Test)
                && Compare(a.IfTrue, b.IfTrue)
                && Compare(a.IfFalse, b.IfFalse);

        private static bool CompareDebugInfo(DebugInfoExpression a, DebugInfoExpression b) =>
            Equals(a.Document, b.Document)
                && Equals(a.StartLine, b.StartLine)
                && Equals(a.StartColumn, b.StartColumn)
                && Equals(a.EndLine, b.EndLine)
                && Equals(a.IsClear, b.IsClear);

        private static bool CompareConstant(ConstantExpression a, ConstantExpression b)
            => Equals(a.Value, b.Value);

        private bool CompareDynamic(DynamicExpression a, DynamicExpression b) =>
            Equals(a.DelegateType, b.DelegateType)
                && Equals(a.Binder, b.Binder)
                && CompareExpressionList(a.Arguments, b.Arguments);

        private bool CompareGoto(GotoExpression a, GotoExpression b)
        {
            gotos ??= new List<(LabelTarget, LabelTarget)>();
            gotos.Add((a.Target, b.Target));
            return a.Kind == b.Kind && Compare(a.Value, b.Value);
        }

        private bool CompareIndex(IndexExpression a, IndexExpression b)
            => Equals(a.Indexer, b.Indexer)
                && Compare(a.Object, b.Object)
                && CompareExpressionList(a.Arguments, b.Arguments);

        private bool CompareInvocation(InvocationExpression a, InvocationExpression b)
            => Compare(a.Expression, b.Expression)
                && CompareExpressionList(a.Arguments, b.Arguments);

        private bool CompareLabel(LabelExpression a, LabelExpression b) =>
            DefineLabelAndCompare(a.Target, b.Target)
            && Compare(a.DefaultValue, b.DefaultValue);

        private bool CompareLambda(LambdaExpression a, LambdaExpression b)
        {
            if (a.Parameters.Count != b.Parameters.Count)
                return false;

            EnterParameters(a.Parameters, b.Parameters);

            try
            {
                return Compare(a.Body, b.Body);
            }
            finally
            {
                ExitParameters(a.Parameters);
            }
        }

        private bool CompareListInit(ListInitExpression a, ListInitExpression b)
            => Compare(a.NewExpression, b.NewExpression)
                && CompareElementInitList(a.Initializers, b.Initializers);

        private bool CompareLoop(LoopExpression a, LoopExpression b) =>
            DefineLabelAndCompare(a.BreakLabel, b.BreakLabel)
                && DefineLabelAndCompare(a.ContinueLabel, b.ContinueLabel)
                && Compare(a.Body, b.Body);

        private bool CompareMember(MemberExpression a, MemberExpression b)
            => Equals(a.Member, b.Member)
                && Compare(a.Expression, b.Expression);

        private bool CompareMemberInit(MemberInitExpression a, MemberInitExpression b)
            => Compare(a.NewExpression, b.NewExpression)
                && CompareMemberBindingList(a.Bindings, b.Bindings);

        private bool CompareMethodCall(MethodCallExpression a, MethodCallExpression b)
            => Equals(a.Method, b.Method)
                && Compare(a.Object, b.Object)
                && CompareExpressionList(a.Arguments, b.Arguments);

        private bool CompareNewArray(NewArrayExpression a, NewArrayExpression b)
            => CompareExpressionList(a.Expressions, b.Expressions);

        private bool CompareNew(NewExpression a, NewExpression b)
            => Equals(a.Constructor, b.Constructor)
                && CompareExpressionList(a.Arguments, b.Arguments)
                && CompareMemberList(a.Members, b.Members);

        private bool CompareParameter(ParameterExpression a, ParameterExpression b) =>
            scope != null && scope.TryGetValue(a, out var mapped)
                ? mapped == b
                : a == b;

        private bool CompareRuntimeVariables(RuntimeVariablesExpression a, RuntimeVariablesExpression b)
            => CompareExpressionList(a.Variables, b.Variables);

        private bool CompareSwitch(SwitchExpression a, SwitchExpression b)
            => Equals(a.Comparison, b.Comparison)
                && Compare(a.SwitchValue, b.SwitchValue)
                && Compare(a.DefaultBody, b.DefaultBody)
                && CompareSwitchCaseList(a.Cases, b.Cases);

        private bool CompareTry(TryExpression a, TryExpression b)
            => Compare(a.Body, b.Body)
                && Compare(a.Fault, b.Fault)
                && Compare(a.Finally, b.Finally)
                && CompareCatchBlockList(a.Handlers, b.Handlers);

        private bool CompareTypeBinary(TypeBinaryExpression a, TypeBinaryExpression b)
            => a.TypeOperand == b.TypeOperand
                && Compare(a.Expression, b.Expression);

        private bool CompareUnary(UnaryExpression a, UnaryExpression b)
            => Equals(a.Method, b.Method)
                && a.IsLifted == b.IsLifted
                && a.IsLiftedToNull == b.IsLiftedToNull
                && Compare(a.Operand, b.Operand);

        private void EnterParameters(IReadOnlyList<ParameterExpression> a, IReadOnlyList<ParameterExpression> b)
        {
            for (var i = 0; i < a.Count; i++)
                AddParameter(a[i], b[i]);
        }

        private void ExitParameters(IReadOnlyList<ParameterExpression> a)
        {
            foreach (ParameterExpression v in a)
                scope.Remove(v);
        }

        private bool DefineLabelAndCompare(LabelTarget? a, LabelTarget? b)
        {
            if (a == null && b == null)
                return true;

            if (a == null || b == null)
                return false;

            labels ??= new Dictionary<LabelTarget, LabelTarget>();
            labels[a] = b;
            return true;
        }

        private bool CompareExpressionList(IReadOnlyList<Expression> a, IReadOnlyList<Expression> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!Compare(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareMemberList(IReadOnlyList<MemberInfo>? a, IReadOnlyList<MemberInfo>? b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!Equals(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareMemberBindingList(IReadOnlyList<MemberBinding> a, IReadOnlyList<MemberBinding> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!CompareBinding(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareBinding(MemberBinding a, MemberBinding b)
        {
            if (a == b)
                return true;

            if (a == null || b == null)
                return false;

            if (a.BindingType != b.BindingType)
                return false;

            if (!Equals(a.Member, b.Member))
                return false;

            return a switch
            {
                MemberAssignment aMemberAssignment => Compare(aMemberAssignment.Expression, ((MemberAssignment)b).Expression),
                MemberListBinding aMemberListBinding => CompareElementInitList(aMemberListBinding.Initializers, ((MemberListBinding)b).Initializers),
                MemberMemberBinding aMemberMemberBinding => CompareMemberBindingList(aMemberMemberBinding.Bindings, ((MemberMemberBinding)b).Bindings),
                _ => throw new InvalidOperationException($"Unable to compare MemberBindings of type {a.BindingType}"),
            };
        }

        private bool CompareElementInitList(IReadOnlyList<ElementInit> a, IReadOnlyList<ElementInit> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!CompareElementInit(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareElementInit(ElementInit a, ElementInit b)
            => Equals(a.AddMethod, b.AddMethod)
                && CompareExpressionList(a.Arguments, b.Arguments);

        private bool CompareSwitchCaseList(IReadOnlyList<SwitchCase> a, IReadOnlyList<SwitchCase> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!CompareSwitchCase(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareSwitchCase(SwitchCase a, SwitchCase b)
            => Compare(a.Body, b.Body)
                && CompareExpressionList(a.TestValues, b.TestValues);

        private bool CompareCatchBlockList(IReadOnlyList<CatchBlock> a, IReadOnlyList<CatchBlock> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;

            for (int i = 0, n = a.Count; i < n; i++)
            {
                if (!CompareCatchBlock(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompareCatchBlock(CatchBlock a, CatchBlock b)
        {
            bool hasVariable = a.Variable != null || b.Variable != null;

            if (hasVariable)
            {
                if (a.Variable == null || b.Variable == null)
                {
                    return false;
                }

                EnterParameters(new[] { a.Variable }, new[] { b.Variable });
            }

            try
            {
                return Equals(a.Test, b.Test)
                    && Compare(a.Body, b.Body)
                    && Compare(a.Filter, b.Filter);
            }
            finally
            {
                if (hasVariable)
                {
                    ExitParameters(new[] { a.Variable! });
                }
            }
        }

        private bool ValidateLabels()
        {
            if (labels != null || gotos != null)
            {
                if (labels == null || gotos == null)
                    return false;

                foreach ((var leftLabel, var rightLabel) in gotos)
                {
                    if (labels[leftLabel] != rightLabel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
