// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToImperative.Utils.Nuqleon
{
    /// <summary>
    /// Base class for expression equality comparer implementations. Default behavior matches trees in a structural fashion.
    /// </summary>
    public class ExpressionEqualityComparator
        : IEqualityComparer<Expression>, IEqualityComparer<MemberBinding>, IEqualityComparer<ElementInit>, IEqualityComparer<CatchBlock>, IEqualityComparer<SwitchCase>, IEqualityComparer<CallSiteBinder>
    {
        private Stack<IReadOnlyList<ParameterExpression?>>? _environmentLeft;
        private Stack<IReadOnlyList<ParameterExpression?>>? _environmentRight;
        private LabelData? _labelData;

        private Stack<IReadOnlyList<ParameterExpression?>> EnvironmentLeft => _environmentLeft ??= new();
        private Stack<IReadOnlyList<ParameterExpression?>> EnvironmentRight => _environmentRight ??= new();
        private LabelData BranchTrackingData => _labelData ??= new();

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        public virtual bool Equals(Expression? x, Expression? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x.NodeType != y.NodeType)
            {
                return false;
            }

            var res = false;

            switch (x.NodeType)
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
                    res = EqualsBinary((BinaryExpression)x, (BinaryExpression)y);
                    break;

                case ExpressionType.Conditional:
                    res = EqualsConditional((ConditionalExpression)x, (ConditionalExpression)y);
                    break;

                case ExpressionType.Constant:
                    res = EqualsConstant((ConstantExpression)x, (ConstantExpression)y);
                    break;

                case ExpressionType.Invoke:
                    res = EqualsInvocation((InvocationExpression)x, (InvocationExpression)y);
                    break;

                case ExpressionType.Lambda:
                    res = EqualsLambda((LambdaExpression)x, (LambdaExpression)y);
                    break;

                case ExpressionType.ListInit:
                    res = EqualsListInit((ListInitExpression)x, (ListInitExpression)y);
                    break;

                case ExpressionType.MemberAccess:
                    res = EqualsMember((MemberExpression)x, (MemberExpression)y);
                    break;

                case ExpressionType.MemberInit:
                    res = EqualsMemberInit((MemberInitExpression)x, (MemberInitExpression)y);
                    break;

                case ExpressionType.Call:
                    res = EqualsMethodCall((MethodCallExpression)x, (MethodCallExpression)y);
                    break;

                case ExpressionType.New:
                    res = EqualsNew((NewExpression)x, (NewExpression)y);
                    break;

                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
                    res = EqualsNewArray((NewArrayExpression)x, (NewArrayExpression)y);
                    break;

                case ExpressionType.Parameter:
                    res = EqualsParameter((ParameterExpression)x, (ParameterExpression)y);
                    break;

                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    res = EqualsTypeBinary((TypeBinaryExpression)x, (TypeBinaryExpression)y);
                    break;

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
                    res = EqualsUnary((UnaryExpression)x, (UnaryExpression)y);
                    break;

                case ExpressionType.Block:
                    res = EqualsBlock((BlockExpression)x, (BlockExpression)y);
                    break;

                case ExpressionType.DebugInfo:
                    res = EqualsDebugInfo((DebugInfoExpression)x, (DebugInfoExpression)y);
                    break;

                case ExpressionType.Default:
                    res = EqualsDefault((DefaultExpression)x, (DefaultExpression)y);
                    break;

                case ExpressionType.Dynamic:
                    res = EqualsDynamic((DynamicExpression)x, (DynamicExpression)y);
                    break;

                case ExpressionType.Extension:
                    res = EqualsExtension(x, y);
                    break;

                case ExpressionType.Goto:
                    res = EqualsGoto((GotoExpression)x, (GotoExpression)y);
                    break;

                case ExpressionType.Index:
                    res = EqualsIndex((IndexExpression)x, (IndexExpression)y);
                    break;

                case ExpressionType.Label:
                    res = EqualsLabel((LabelExpression)x, (LabelExpression)y);
                    break;

                case ExpressionType.Loop:
                    res = EqualsLoop((LoopExpression)x, (LoopExpression)y);
                    break;

                case ExpressionType.RuntimeVariables:
                    res = EqualsRuntimeVariables((RuntimeVariablesExpression)x, (RuntimeVariablesExpression)y);
                    break;

                case ExpressionType.Switch:
                    res = EqualsSwitch((SwitchExpression)x, (SwitchExpression)y);
                    break;

                case ExpressionType.Try:
                    res = EqualsTry((TryExpression)x, (TryExpression)y);
                    break;
            }

            return res;
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsBinary(BinaryExpression x, BinaryExpression y)
        {
            return
                Equals(x.Method, y.Method) &&
                Equals(x.Left, y.Left) &&
                Equals(x.Right, y.Right) &&
                Equals(x.Conversion, y.Conversion);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsConditional(ConditionalExpression x, ConditionalExpression y)
        {
            return
                Equals(x.Test, y.Test) &&
                Equals(x.IfTrue, y.IfTrue) &&
                Equals(x.IfFalse, y.IfFalse);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsConstant(ConstantExpression x, ConstantExpression y)
        {
            return
                Equals(x.Type, y.Type) &&
                EqualsConstant(x.Value, y.Value);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsInvocation(InvocationExpression x, InvocationExpression y)
        {
            return
                Equals(x.Expression, y.Expression) &&
                Equals(x.Arguments, y.Arguments);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsLambda(LambdaExpression x, LambdaExpression y)
        {
            if (x.Parameters.Count != y.Parameters.Count)
            {
                return false;
            }

            EqualsPush(x.Parameters, y.Parameters);

            var res =
                Equals(x.Type, y.Type) &&
                Equals(x.Body, y.Body);

            res = res && Equals(x.TailCall, y.TailCall);

            EqualsPop();

            return res;
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsListInit(ListInitExpression x, ListInitExpression y)
        {
            return
                Equals(x.NewExpression, y.NewExpression) &&
                Equals(x.Initializers, y.Initializers);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsMember(MemberExpression x, MemberExpression y)
        {
            return
                Equals(x.Member, y.Member) &&
                Equals(x.Expression, y.Expression);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsMemberInit(MemberInitExpression x, MemberInitExpression y)
        {
            return
                Equals(x.NewExpression, y.NewExpression) &&
                Equals(x.Bindings, y.Bindings);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsMethodCall(MethodCallExpression x, MethodCallExpression y)
        {
            return
                Equals(x.Method, y.Method) &&
                Equals(x.Object, y.Object) &&
                Equals(x.Arguments, y.Arguments);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsNew(NewExpression x, NewExpression y)
        {
            return
                Equals(x.Type, y.Type) && // Constructor can be null
                Equals(x.Constructor, y.Constructor) &&
                Equals(x.Arguments, y.Arguments) &&
                Equals(x.Members, y.Members);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsNewArray(NewArrayExpression x, NewArrayExpression y)
        {
            return
                Equals(x.Type, y.Type) &&
                Equals(x.Expressions, y.Expressions);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsParameter(ParameterExpression x, ParameterExpression y)
        {
            var l = Find(x, EnvironmentLeft);
            var r = Find(y, EnvironmentRight);

            if (l == null && r == null)
            {
                return EqualsGlobalParameter(x, y);
            }

            if (l == null || r == null)
            {
                return false;
            }

            var res = l.Value.Scope == r.Value.Scope && l.Value.Index == r.Value.Index;

            res = res && Equals(x.IsByRef, y.IsByRef);

            return res;
        }

        /// <summary>
        /// Checks whether the two given global parameter expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsGlobalParameter(ParameterExpression x, ParameterExpression y) => ReferenceEquals(x, y);

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsTypeBinary(TypeBinaryExpression x, TypeBinaryExpression y)
        {
            return
                Equals(x.TypeOperand, y.TypeOperand) &&
                Equals(x.Expression, y.Expression);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsUnary(UnaryExpression x, UnaryExpression y)
        {
            switch (x.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.TypeAs:
                case ExpressionType.Throw:
                case ExpressionType.Unbox:
                    if (!Equals(x.Type, y.Type))
                    {
                        return false;
                    }
                    break;
            }

            return
                Equals(x.Method, y.Method) &&
                Equals(x.Operand, y.Operand);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsBlock(BlockExpression x, BlockExpression y)
        {
            if (x.Variables.Count != y.Variables.Count)
            {
                return false;
            }

            EqualsPush(x.Variables, y.Variables);

            var res =
                Equals(x.Type, y.Type) &&
                Equals(x.Expressions, y.Expressions);

            EqualsPop();

            return res;
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsDebugInfo(DebugInfoExpression x, DebugInfoExpression y)
        {
            throw new NotImplementedException("Equality of DebugInfoExpression to be supplied by derived types.");
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsDefault(DefaultExpression x, DefaultExpression y)
        {
            return Equals(x.Type, y.Type);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsDynamic(DynamicExpression x, DynamicExpression y)
        {
            return
                Equals(x.Type, y.Type) &&
                Equals(x.DelegateType, y.DelegateType) &&
                Equals(x.Binder, y.Binder) &&
                Equals(x.Arguments, y.Arguments);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsGoto(GotoExpression x, GotoExpression y)
        {
            return
                GotoLabelAndCheck(x.Target, y.Target) &&
                Equals(x.Kind, y.Kind) &&
                Equals(x.Value, y.Value);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsIndex(IndexExpression x, IndexExpression y)
        {
            return
                Equals(x.Indexer, y.Indexer) &&
                Equals(x.Object, y.Object) &&
                Equals(x.Arguments, y.Arguments);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsLabel(LabelExpression x, LabelExpression y)
        {
            return
                DefineLabelAndCheck(x.Target, y.Target) &&
                Equals(x.DefaultValue, y.DefaultValue);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsLoop(LoopExpression x, LoopExpression y)
        {
            return
                DefineLabelAndCheck(x.BreakLabel, y.BreakLabel) &&
                DefineLabelAndCheck(x.ContinueLabel, y.ContinueLabel) &&
                Equals(x.Body, y.Body);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsRuntimeVariables(RuntimeVariablesExpression x, RuntimeVariablesExpression y)
        {
            return Equals(x.Variables, y.Variables);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsSwitch(SwitchExpression x, SwitchExpression y)
        {
            return
                Equals(x.SwitchValue, y.SwitchValue) &&
                Equals(x.Comparison, y.Comparison) &&
                Equals(x.Cases, y.Cases) &&
                Equals(x.DefaultBody, y.DefaultBody);
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsTry(TryExpression x, TryExpression y)
        {
            return
                Equals(x.Body, y.Body) &&
                Equals(x.Handlers, y.Handlers) &&
                Equals(x.Fault, y.Fault) &&
                Equals(x.Finally, y.Finally);
        }

        /// <summary>
        /// Checks whether the two given switch cases are equal.
        /// </summary>
        /// <param name="x">First switch cases.</param>
        /// <param name="y">Second switch cases.</param>
        /// <returns>true if both switch cases are equal; otherwise, false.</returns>
        public virtual bool Equals(SwitchCase? x, SwitchCase? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return
                Equals(x.TestValues, y.TestValues) &&
                Equals(x.Body, y.Body);
        }

        /// <summary>
        /// Checks whether the two given catch blocks are equal.
        /// </summary>
        /// <param name="x">First catch block.</param>
        /// <param name="y">Second catch block.</param>
        /// <returns>true if both catch blocks are equal; otherwise, false.</returns>
        public virtual bool Equals(CatchBlock? x, CatchBlock? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            EqualsPush(new[] { x.Variable }, new[] { y.Variable });

            var res =
                Equals(x.Body, y.Body) &&
                Equals(x.Test, y.Test) &&
                Equals(x.Filter, y.Filter);

            EqualsPop();

            return res;
        }

        /// <summary>
        /// Checks whether the two given call site binders are equal.
        /// </summary>
        /// <param name="x">First call site binder.</param>
        /// <param name="y">Second call site binder.</param>
        /// <returns>true if both call site binders are equal; otherwise, false.</returns>
        public virtual bool Equals(CallSiteBinder? x, CallSiteBinder? y)
        {
            return EqualityComparer<CallSiteBinder>.Default.Equals(x, y);
        }

        /// <summary>
        /// Checks whether the two given extension expressions are equal.
        /// </summary>
        /// <param name="x">First extension expression.</param>
        /// <param name="y">Second extension expression.</param>
        /// <returns>true if both extension expressions are equal; otherwise, false.</returns>
        protected virtual bool EqualsExtension(Expression x, Expression y)
        {
            throw new NotImplementedException("Equality of extension nodes to be supplied by derived types.");
        }

        /// <summary>
        /// Checks whether the two given member binders are equal.
        /// </summary>
        /// <param name="x">First member binder.</param>
        /// <param name="y">Second member binder.</param>
        /// <returns>true if both member binders are equal; otherwise, false.</returns>
        public virtual bool Equals(MemberBinding? x, MemberBinding? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x.BindingType != y.BindingType)
            {
                return false;
            }

            var res = false;

            switch (x.BindingType)
            {
                case MemberBindingType.Assignment:
                    res = EqualsMemberAssignment((MemberAssignment)x, (MemberAssignment)y);
                    break;
                case MemberBindingType.ListBinding:
                    res = EqualsMemberListBinding((MemberListBinding)x, (MemberListBinding)y);
                    break;
                case MemberBindingType.MemberBinding:
                    res = EqualsMemberMemberBinding((MemberMemberBinding)x, (MemberMemberBinding)y);
                    break;
            }

            return res;
        }

        /// <summary>
        /// Checks whether the two given member assignments are equal.
        /// </summary>
        /// <param name="x">First member assignment.</param>
        /// <param name="y">Second member assignment.</param>
        /// <returns>true if both member assignments are equal; otherwise, false.</returns>
        protected virtual bool EqualsMemberAssignment(MemberAssignment x, MemberAssignment y)
        {
            return
                Equals(x.Member, y.Member) &&
                Equals(x.Expression, y.Expression);
        }

        /// <summary>
        /// Checks whether the two given nested member bindings are equal.
        /// </summary>
        /// <param name="x">First nested member binding.</param>
        /// <param name="y">Second nested member binding.</param>
        /// <returns>true if both nested member bindings are equal; otherwise, false.</returns>
        protected virtual bool EqualsMemberMemberBinding(MemberMemberBinding x, MemberMemberBinding y)
        {
            return
                Equals(x.Member, y.Member) &&
                Equals(x.Bindings, y.Bindings);
        }

        /// <summary>
        /// Checks whether the two given member list bindings are equal.
        /// </summary>
        /// <param name="x">First member list binding.</param>
        /// <param name="y">Second member list binding.</param>
        /// <returns>true if both member list bindings are equal; otherwise, false.</returns>
        protected virtual bool EqualsMemberListBinding(MemberListBinding x, MemberListBinding y)
        {
            return
                Equals(x.Member, y.Member) &&
                Equals(x.Initializers, y.Initializers);
        }

        /// <summary>
        /// Checks whether the two given element initializers are equal.
        /// </summary>
        /// <param name="x">First element initializer.</param>
        /// <param name="y">Second element initializer.</param>
        /// <returns>true if both element initializers are equal; otherwise, false.</returns>
        public virtual bool Equals(ElementInit? x, ElementInit? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            return
                Equals(x.AddMethod, y.AddMethod) &&
                Equals(x.Arguments, y.Arguments);
        }

        /// <summary>
        /// Checks whether the two given members are equal.
        /// </summary>
        /// <param name="x">First member.</param>
        /// <param name="y">Second member.</param>
        /// <returns>true if both members are equal; otherwise, false.</returns>
        protected bool Equals(MemberInfo? x, MemberInfo? y) => EqualityComparer<MemberInfo?>.Default.Equals(x, y);

        /// <summary>
        /// Checks whether the two given member sequences are equal.
        /// </summary>
        /// <param name="x">First member sequence.</param>
        /// <param name="y">Second member sequence.</param>
        /// <returns>true if both member sequences are equal; otherwise, false.</returns>
        protected bool Equals(ReadOnlyCollection<MemberInfo>? x, ReadOnlyCollection<MemberInfo>? y) => SequenceEqual(x, y, EqualityComparer<MemberInfo?>.Default);

        /// <summary>
        /// Checks whether the two given types are equal.
        /// </summary>
        /// <param name="x">First type.</param>
        /// <param name="y">Second type.</param>
        /// <returns>true if both types are equal; otherwise, false.</returns>
        protected bool Equals(Type x, Type y) => EqualityComparer<Type>.Default.Equals(x, y);

        private static bool Equals(bool x, bool y) => x == y;

        private bool EqualsConstant(object? x, object? y) => EqualityComparer<object?>.Default.Equals(x, y);

        /// <summary>
        /// Checks whether the two given expression sequences are equal.
        /// </summary>
        /// <param name="x">First expression sequence.</param>
        /// <param name="y">Second expression sequence.</param>
        /// <returns>true if both expression sequences are equal; otherwise, false.</returns>
        protected bool Equals(ReadOnlyCollection<Expression> x, ReadOnlyCollection<Expression> y) => SequenceEqual(x, y, this);

        /// <summary>
        /// Checks whether the two given parameter expression sequences are equal.
        /// </summary>
        /// <param name="x">First parameter expression sequence.</param>
        /// <param name="y">Second parameter expression sequence.</param>
        /// <returns>true if both parameter expression sequences are equal; otherwise, false.</returns>
        protected bool Equals(ReadOnlyCollection<ParameterExpression> x, ReadOnlyCollection<ParameterExpression> y) => SequenceEqual(x, y, this);

        /// <summary>
        /// Checks whether the two given member binding sequences are equal.
        /// </summary>
        /// <param name="x">First member binding sequence.</param>
        /// <param name="y">Second member binding sequence.</param>
        /// <returns>true if both member binding sequences are equal; otherwise, false.</returns>
        protected bool Equals(ReadOnlyCollection<MemberBinding> x, ReadOnlyCollection<MemberBinding> y) => SequenceEqual(x, y, this);

        /// <summary>
        /// Checks whether the two given element initializer sequences are equal.
        /// </summary>
        /// <param name="x">First element initializer sequence.</param>
        /// <param name="y">Second element initializer sequence.</param>
        /// <returns>true if both element initializer sequences are equal; otherwise, false.</returns>
        protected bool Equals(ReadOnlyCollection<ElementInit> x, ReadOnlyCollection<ElementInit> y) => SequenceEqual(x, y, this);

        /// <summary>
        /// Checks whether the two given sequences are equal.
        /// </summary>
        /// <typeparam name="T">Element type of the sequences.</typeparam>
        /// <param name="first">First sequence to compare.</param>
        /// <param name="second">Second sequence to compare.</param>
        /// <param name="comparer">Equality comparer for elements.</param>
        /// <returns>true if both sequences are equal; otherwise, false.</returns>
        private static bool SequenceEqual<T>(ReadOnlyCollection<T>? first, ReadOnlyCollection<T>? second, IEqualityComparer<T> comparer)
        {
            if (first == null && second == null)
            {
                return true;
            }

            if (first == null || second == null)
            {
                return false;
            }

            var n = first.Count;

            if (n != second.Count)
            {
                return false;
            }

            for (var i = 0; i < n; i++)
            {
                if (!comparer.Equals(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether the two given switch case sequences are equal.
        /// </summary>
        /// <param name="x">First switch case sequence.</param>
        /// <param name="y">Second switch case sequence.</param>
        /// <returns>true if both switch case sequences are equal; otherwise, false.</returns>
        protected bool Equals(ReadOnlyCollection<SwitchCase> x, ReadOnlyCollection<SwitchCase> y) => SequenceEqual(x, y, this);

        /// <summary>
        /// Checks whether the two given catch block sequences are equal.
        /// </summary>
        /// <param name="x">First catch block sequence.</param>
        /// <param name="y">Second catch block sequence.</param>
        /// <returns>true if both catch block sequences are equal; otherwise, false.</returns>
        protected bool Equals(ReadOnlyCollection<CatchBlock> x, ReadOnlyCollection<CatchBlock> y) => SequenceEqual(x, y, this);

        /// <summary>
        /// Push parameters into the expression environments for equality checks.
        /// </summary>
        /// <param name="x">The left parameters.</param>
        /// <param name="y">The right parameters.</param>
        protected void EqualsPush(IReadOnlyList<ParameterExpression?> x, IReadOnlyList<ParameterExpression?> y)
        {
            EnvironmentLeft.Push(x);
            EnvironmentRight.Push(y);
        }

        /// <summary>
        /// Pop parameters from the expression environments for equality checks.
        /// </summary>
        protected void EqualsPop()
        {
            EnvironmentRight.Pop();
            EnvironmentLeft.Pop();
        }

        private static bool Equals(GotoExpressionKind x, GotoExpressionKind y) => x == y;

        private bool DefineLabelAndCheck(LabelTarget? x, LabelTarget? y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            var labelData = BranchTrackingData;

            UpdateLabelMap(labelData.DefinitionsLeft, x, y);
            UpdateLabelMap(labelData.DefinitionsRight, y, x);

            if (!CheckLabelMap(labelData.GotosLeft, x, y) || !CheckLabelMap(labelData.GotosRight, y, x))
            {
                return false;
            }

            return true;
        }

        private bool GotoLabelAndCheck(LabelTarget x, LabelTarget y)
        {
            var labelData = BranchTrackingData;

            UpdateLabelMap(labelData.GotosLeft, x, y);
            UpdateLabelMap(labelData.GotosRight, y, x);

            if (!CheckLabelMap(labelData.DefinitionsLeft, x, y) || !CheckLabelMap(labelData.DefinitionsRight, y, x))
            {
                return false;
            }

            return true;
        }

        private static void UpdateLabelMap(Dictionary<LabelTarget, HashSet<LabelTarget>> labelMap, LabelTarget source, LabelTarget target)
        {
            if (!labelMap.TryGetValue(source, out HashSet<LabelTarget>? labels))
            {
                labelMap[source] = labels = new HashSet<LabelTarget>();
            }

            labels.Add(target);
        }

        private static bool CheckLabelMap(Dictionary<LabelTarget, HashSet<LabelTarget>> labelMap, LabelTarget source, LabelTarget target)
        {
            if (labelMap.TryGetValue(source, out HashSet<LabelTarget>? labels) && !labels.Contains(target))
            {
                return false;
            }

            return true;
        }

        private static ParameterIndex? Find(ParameterExpression p, Stack<IReadOnlyList<ParameterExpression?>> parameters)
        {
            var scope = 0;
            foreach (var frame in parameters)
            {
                for (int i = 0; i < frame.Count; i++)
                {
                    if (frame[i] == p)
                    {
                        return new ParameterIndex { Scope = scope, Index = i };
                    }
                }

                scope++;
            }

            return null;
        }

        private struct ParameterIndex
        {
            public int Scope;
            public int Index;
        }

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        public virtual int GetHashCode(Expression? obj)
        {
            if (obj == null)
            {
                return 17;
            }

            return obj.NodeType switch
            {
                ExpressionType.Add or
                ExpressionType.AddChecked or
                ExpressionType.And or
                ExpressionType.AndAlso or
                ExpressionType.ArrayIndex or
                ExpressionType.Coalesce or
                ExpressionType.Divide or
                ExpressionType.Equal or
                ExpressionType.ExclusiveOr or
                ExpressionType.GreaterThan or
                ExpressionType.GreaterThanOrEqual or
                ExpressionType.LeftShift or
                ExpressionType.LessThan or
                ExpressionType.LessThanOrEqual or
                ExpressionType.Modulo or
                ExpressionType.Multiply or
                ExpressionType.MultiplyChecked or
                ExpressionType.NotEqual or
                ExpressionType.Or or
                ExpressionType.OrElse or
                ExpressionType.Power or
                ExpressionType.RightShift or
                ExpressionType.Subtract or
                ExpressionType.SubtractChecked or
                ExpressionType.AddAssign or
                ExpressionType.AddAssignChecked or
                ExpressionType.AndAssign or
                ExpressionType.Assign or
                ExpressionType.DivideAssign or
                ExpressionType.ExclusiveOrAssign or
                ExpressionType.LeftShiftAssign or
                ExpressionType.ModuloAssign or
                ExpressionType.MultiplyAssign or
                ExpressionType.MultiplyAssignChecked or
                ExpressionType.OrAssign or
                ExpressionType.PowerAssign or
                ExpressionType.RightShiftAssign or
                ExpressionType.SubtractAssign or
                ExpressionType.SubtractAssignChecked => GetHashCodeBinary((BinaryExpression)obj),
                ExpressionType.Conditional => GetHashCodeConditional((ConditionalExpression)obj),
                ExpressionType.Constant => GetHashCodeConstant((ConstantExpression)obj),
                ExpressionType.Invoke => GetHashCodeInvocation((InvocationExpression)obj),
                ExpressionType.Lambda => GetHashCodeLambda((LambdaExpression)obj),
                ExpressionType.ListInit => GetHashCodeListInit((ListInitExpression)obj),
                ExpressionType.MemberAccess => GetHashCodeMember((MemberExpression)obj),
                ExpressionType.MemberInit => GetHashCodeMemberInit((MemberInitExpression)obj),
                ExpressionType.Call => GetHashCodeMethodCall((MethodCallExpression)obj),
                ExpressionType.New => GetHashCodeNew((NewExpression)obj),
                ExpressionType.NewArrayBounds or
                ExpressionType.NewArrayInit => GetHashCodeNewArray((NewArrayExpression)obj),
                ExpressionType.Parameter => GetHashCodeParameter((ParameterExpression)obj),
                ExpressionType.TypeIs or
                ExpressionType.TypeEqual => GetHashCodeTypeBinary((TypeBinaryExpression)obj),
                ExpressionType.ArrayLength or
                ExpressionType.Convert or
                ExpressionType.ConvertChecked or
                ExpressionType.Negate or
                ExpressionType.NegateChecked or
                ExpressionType.Not or
                ExpressionType.Quote or
                ExpressionType.TypeAs or
                ExpressionType.UnaryPlus or
                ExpressionType.Decrement or
                ExpressionType.Increment or
                ExpressionType.IsFalse or
                ExpressionType.IsTrue or
                ExpressionType.OnesComplement or
                ExpressionType.PostDecrementAssign or
                ExpressionType.PostIncrementAssign or
                ExpressionType.PreDecrementAssign or
                ExpressionType.PreIncrementAssign or
                ExpressionType.Throw or
                ExpressionType.Unbox => GetHashCodeUnary((UnaryExpression)obj),
                ExpressionType.Block => GetHashCodeBlock((BlockExpression)obj),
                ExpressionType.Default => GetHashCodeDefault((DefaultExpression)obj),
                ExpressionType.Extension => GetHashCodeExtension(obj),
                ExpressionType.Goto => GetHashCodeGoto((GotoExpression)obj),
                ExpressionType.Index => GetHashCodeIndex((IndexExpression)obj),
                ExpressionType.Label => GetHashCodeLabel((LabelExpression)obj),
                ExpressionType.Loop => GetHashCodeLoop((LoopExpression)obj),
                ExpressionType.Switch => GetHashCodeSwitch((SwitchExpression)obj),
                ExpressionType.Try => GetHashCodeTry((TryExpression)obj),
                ExpressionType.DebugInfo => GetHashCodeDebugInfo((DebugInfoExpression)obj),
                ExpressionType.Dynamic => GetHashCodeDynamic((DynamicExpression)obj),
                ExpressionType.RuntimeVariables => GetHashCodeRuntimeVariables((RuntimeVariablesExpression)obj),
                _ => 1979,
            };
        }

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeBinary(BinaryExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.NodeType),
                GetHashCode(obj.Left),
                GetHashCode(obj.Right),
                GetHashCode(obj.Conversion),
                GetHashCode(obj.Method)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeConditional(ConditionalExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Test),
                GetHashCode(obj.IfTrue),
                GetHashCode(obj.IfFalse)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeConstant(ConstantExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Type),
                GetHashCodeConstant(obj.Value)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeInvocation(InvocationExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Expression),
                GetHashCode(obj.Arguments)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeLambda(LambdaExpression obj)
        {
            GetHashCodePush(obj.Parameters);

            var res = HashCode.Combine(
                GetHashCode(obj.Body),
                GetHashCode(obj.Type),
                obj.TailCall.GetHashCode()
            );

            GetHashCodePop();

            return res;
        }

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeListInit(ListInitExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.NewExpression),
                GetHashCode(obj.Initializers)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeMember(MemberExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Expression),
                GetHashCode(obj.Member)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeMemberInit(MemberInitExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.NewExpression),
                GetHashCode(obj.Bindings)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeMethodCall(MethodCallExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Object),
                GetHashCode(obj.Method),
                GetHashCode(obj.Arguments)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeNew(NewExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Type),
                GetHashCode(obj.Constructor),
                GetHashCode(obj.Arguments),
                GetHashCode(obj.Members)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeNewArray(NewArrayExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.NodeType),
                GetHashCode(obj.Type),
                GetHashCode(obj.Expressions)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeParameter(ParameterExpression obj)
        {
            var i = 0;
            foreach (var frame in EnvironmentLeft)
            {
                for (int j = 0; j < frame.Count; j++)
                {
                    if (frame[j] == obj)
                    {
                        return i * 37 + j;
                    }
                }

                i++;
            }

            return GetHashCodeGlobalParameter(obj);
        }

        /// <summary>
        /// Gets a hash code for a global parameter.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeGlobalParameter(ParameterExpression obj) =>
            obj.GetHashCode();

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeTypeBinary(TypeBinaryExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.NodeType),
                GetHashCode(obj.Expression),
                GetHashCode(obj.TypeOperand)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeUnary(UnaryExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.NodeType),
                GetHashCode(obj.Operand),
                GetHashCode(obj.Method));

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeBlock(BlockExpression obj)
        {
            GetHashCodePush(obj.Variables);

            var res = HashCode.Combine(
                GetHashCode(obj.Expressions),
                GetHashCode(obj.Type)
            );

            GetHashCodePop();

            return res;
        }

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeDebugInfo(DebugInfoExpression obj)
        {
            throw new NotImplementedException("Hash code of DebugInfoExpression to be supplied by derived types.");
        }

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeDefault(DefaultExpression obj) =>
            GetHashCode(obj.Type);

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeDynamic(DynamicExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Arguments),
                GetHashCode(obj.Type),
                GetHashCode(obj.DelegateType),
                GetHashCode(obj.Binder)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeGoto(GotoExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Value),
                GetHashCode(obj.Target),
                (int)obj.Kind
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeIndex(IndexExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Object),
                GetHashCode(obj.Indexer),
                GetHashCode(obj.Arguments)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeLabel(LabelExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.DefaultValue),
                GetHashCode(obj.Target)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeLoop(LoopExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.ContinueLabel),
                GetHashCode(obj.BreakLabel),
                GetHashCode(obj.Body)
            );

        /// <summary>
        /// Gets a hash code for the given label target.
        /// </summary>
        /// <param name="obj">Label target to compute a hash code for.</param>
        /// <returns>Hash code for the given label target.</returns>
        protected virtual int GetHashCode(LabelTarget? obj) =>
            obj == null ? 17 : GetHashCode(obj.Type);

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeRuntimeVariables(RuntimeVariablesExpression obj) =>
            GetHashCode(obj.Variables);

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeSwitch(SwitchExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.SwitchValue),
                GetHashCode(obj.Comparison),
                GetHashCode(obj.Cases),
                GetHashCode(obj.DefaultBody)
            );

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        protected virtual int GetHashCodeTry(TryExpression obj) =>
            HashCode.Combine(
                GetHashCode(obj.Body),
                GetHashCode(obj.Handlers),
                GetHashCode(obj.Fault),
                GetHashCode(obj.Finally)
            );

        /// <summary>
        /// Gets a hash code for the given switch case.
        /// </summary>
        /// <param name="obj">Switch case to compute a hash code for.</param>
        /// <returns>Hash code for the given switch case.</returns>
        public virtual int GetHashCode(SwitchCase obj) =>
            HashCode.Combine(
                GetHashCode(obj.TestValues),
                GetHashCode(obj.Body)
            );

        /// <summary>
        /// Gets a hash code for the given catch block.
        /// </summary>
        /// <param name="obj">Catch block to compute a hash code for.</param>
        /// <returns>Hash code for the given catch block.</returns>
        public virtual int GetHashCode(CatchBlock obj)
        {
            GetHashCodePush(new[] { obj.Variable });

            var res = HashCode.Combine(
                GetHashCode(obj.Body),
                GetHashCode(obj.Test),
                GetHashCode(obj.Filter)
            );

            GetHashCodePop();

            return res;
        }

        /// <summary>
        /// Gets a hash code for the given call site binder.
        /// </summary>
        /// <param name="obj">Call site binder to compute a hash code for.</param>
        /// <returns>Hash code for the given call site binder.</returns>
        public virtual int GetHashCode(CallSiteBinder obj) => EqualityComparer<CallSiteBinder>.Default.GetHashCode(obj);

        /// <summary>
        /// Gets a hash code for the given extension expression.
        /// </summary>
        /// <param name="obj">Extension expression to compute a hash code for.</param>
        /// <returns>Hash code for the given extension expression.</returns>
        protected virtual int GetHashCodeExtension(Expression obj)
        {
            throw new NotImplementedException("Hash code of extension nodes to be supplied by derived types.");
        }

        /// <summary>
        /// Gets a hash code for the given member binding.
        /// </summary>
        /// <param name="obj">Member binding to compute a hash code for.</param>
        /// <returns>Hash code for the given member binding.</returns>
        public virtual int GetHashCode(MemberBinding obj)
        {
            var hashCode = new HashCode();

            hashCode.Add((int)obj.BindingType);

            switch (obj.BindingType)
            {
                case MemberBindingType.Assignment:
                    hashCode.Add(GetHashCodeMemberAssignment((MemberAssignment)obj));
                    break;
                case MemberBindingType.ListBinding:
                    hashCode.Add(GetHashCodeMemberListBinding((MemberListBinding)obj));
                    break;
                case MemberBindingType.MemberBinding:
                    hashCode.Add(GetHashCodeMemberMemberBinding((MemberMemberBinding)obj));
                    break;
            }

            return hashCode.ToHashCode();
        }

        /// <summary>
        /// Gets a hash code for the given member assignment.
        /// </summary>
        /// <param name="obj">Member assignment to compute a hash code for.</param>
        /// <returns>Hash code for the given member assignment.</returns>
        protected virtual int GetHashCodeMemberAssignment(MemberAssignment obj) =>
            HashCode.Combine(
                GetHashCode(obj.Member),
                GetHashCode(obj.Expression)
            );

        /// <summary>
        /// Gets a hash code for the given deep member binding.
        /// </summary>
        /// <param name="obj">Deep member binding to compute a hash code for.</param>
        /// <returns>Hash code for the given deep member binding.</returns>
        protected virtual int GetHashCodeMemberMemberBinding(MemberMemberBinding obj) =>
            HashCode.Combine(
                GetHashCode(obj.Member),
                GetHashCode(obj.Bindings)
            );

        /// <summary>
        /// Gets a hash code for the given member list binding.
        /// </summary>
        /// <param name="obj">Member list binding to compute a hash code for.</param>
        /// <returns>Hash code for the given member list binding.</returns>
        protected virtual int GetHashCodeMemberListBinding(MemberListBinding obj) =>
            HashCode.Combine(
                GetHashCode(obj.Member),
                GetHashCode(obj.Initializers)
            );

        /// <summary>
        /// Gets a hash code for the given element initializer.
        /// </summary>
        /// <param name="obj">Element initializer to compute a hash code for.</param>
        /// <returns>Hash code for the given element initializer.</returns>
        public virtual int GetHashCode(ElementInit obj) =>
            HashCode.Combine(
                GetHashCode(obj.AddMethod),
                GetHashCode(obj.Arguments)
            );

        /// <summary>
        /// Gets a hash code for the given member.
        /// </summary>
        /// <param name="obj">Member to compute a hash code for.</param>
        /// <returns>Hash code for the given member.</returns>
        protected int GetHashCode(MemberInfo? obj) => EqualityComparer<MemberInfo?>.Default.GetHashCode(obj!);

        /// <summary>
        /// Gets a hash code for the given member sequence.
        /// </summary>
        /// <param name="obj">Member sequence to compute a hash code for.</param>
        /// <returns>Hash code for the given member sequence.</returns>
        protected int GetHashCode(ReadOnlyCollection<MemberInfo>? obj)
        {
            unchecked
            {
                var hashCode = new HashCode();

                if (obj != null)
                {
                    for (int i = 0, n = obj.Count; i < n; i++)
                    {
                        hashCode.Add(GetHashCode(obj[i]));
                    }
                }

                return hashCode.ToHashCode();
            }
        }

        /// <summary>
        /// Gets a hash code for the given type.
        /// </summary>
        /// <param name="obj">Type to compute a hash code for.</param>
        /// <returns>Hash code for the given type.</returns>
        protected int GetHashCode(Type obj) => EqualityComparer<Type>.Default.GetHashCode(obj);

        /// <summary>
        /// Push parameters into the expression environment for hash code computation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        protected void GetHashCodePush(IReadOnlyList<ParameterExpression?> parameters) => EnvironmentLeft.Push(parameters);

        /// <summary>
        /// Pop parameters from the expression environments for hash code computation.
        /// </summary>
        protected void GetHashCodePop() => EnvironmentLeft.Pop();

        private static int GetHashCode(ExpressionType obj) => (int)obj;

        private int GetHashCodeConstant(object? obj) => EqualityComparer<object?>.Default.GetHashCode(obj!);


        /// <summary>
        /// Gets a hash code for the given expression sequence.
        /// </summary>
        /// <param name="obj">Expression sequence to compute a hash code for.</param>
        /// <returns>Hash code for the given expression sequence.</returns>
        protected int GetHashCode(ReadOnlyCollection<Expression> obj)
        {
            HashCode h = default;

            foreach (var expression in obj)
            {
                h.Add(GetHashCode(expression));
            }

            return h.ToHashCode();
        }

        /// <summary>
        /// Gets a hash code for the given parameter expression sequence.
        /// </summary>
        /// <param name="obj">Parameter expression sequence to compute a hash code for.</param>
        /// <returns>Hash code for the given parameter expression sequence.</returns>
        protected int GetHashCode(ReadOnlyCollection<ParameterExpression> obj)
        {
            HashCode h = default;

            foreach (var param in obj)
            {
                h.Add(GetHashCode(param));
            }

            return h.ToHashCode();
        }

        /// <summary>
        /// Gets a hash code for the given member binding sequence.
        /// </summary>
        /// <param name="obj">Member binding sequence to compute a hash code for.</param>
        /// <returns>Hash code for the given member binding sequence.</returns>
        protected int GetHashCode(ReadOnlyCollection<MemberBinding> obj)
        {
            HashCode h = default;

            foreach (var member in obj)
            {
                h.Add(GetHashCode(member));
            }

            return h.ToHashCode();
        }

        /// <summary>
        /// Gets a hash code for the given element initializer sequence.
        /// </summary>
        /// <param name="obj">Member sequence to compute a hash code for.</param>
        /// <returns>Hash code for the given element initializer sequence.</returns>
        protected int GetHashCode(ReadOnlyCollection<ElementInit> obj)
        {
            HashCode h = default;

            foreach (var elementInit in obj)
            {
                h.Add(GetHashCode(elementInit));
            }

            return h.ToHashCode();
        }

        /// <summary>
        /// Gets a hash code for the given switch case sequence.
        /// </summary>
        /// <param name="obj">Switch case sequence to compute a hash code for.</param>
        /// <returns>Hash code for the given switch case sequence.</returns>
        protected int GetHashCode(ReadOnlyCollection<SwitchCase> obj)
        {
            HashCode h = default;

            foreach (var switchCase in obj)
            {
                h.Add(GetHashCode(switchCase));
            }

            return h.ToHashCode();
        }

        /// <summary>
        /// Gets a hash code for the given catch block sequence.
        /// </summary>
        /// <param name="obj">Catch block sequence to compute a hash code for.</param>
        /// <returns>Hash code for the given catch block sequence.</returns>
        protected int GetHashCode(ReadOnlyCollection<CatchBlock> obj)
        {
            HashCode h = default;

            foreach (var catchBlock in obj)
            {
                h.Add(GetHashCode(catchBlock));
            }

            return h.ToHashCode();
        }

        private sealed class LabelData
        {
            public Dictionary<LabelTarget, HashSet<LabelTarget>> DefinitionsLeft { get; } = new();

            public Dictionary<LabelTarget, HashSet<LabelTarget>> DefinitionsRight { get; } = new();

            public Dictionary<LabelTarget, HashSet<LabelTarget>> GotosLeft { get; } = new();

            public Dictionary<LabelTarget, HashSet<LabelTarget>> GotosRight { get; } = new();
        }
    }
}