using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// An <see cref="IExprEnumerable"/> that represents how to enumerate over an
    /// <see cref="IEnumerable{T}"/> in an imperative way.
    /// </summary>
    internal class BaseExprEnumerable : ILinearExprEnumerable
    {
        private static readonly MethodInfo MoveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;

        private readonly Expression enumerableValueToInit;
        private readonly ParameterExpression enumeratorVar;
        private readonly ParameterExpression hasNextVar = Expression.Variable(typeof(bool), "hasNext");
        private readonly MethodInfo toEnumeratorMethod;
        private readonly MethodInfo currentProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseExprEnumerable"/> class.
        /// </summary>
        /// <param name="enumerable">An expression representing an <see cref="IEnumerable{T}"/>.</param>
        /// <param name="elementType">The type of the enumerable's elements if available.</param>
        public BaseExprEnumerable(Expression enumerable, Type? elementType = null)
        {
            this.ElementType = elementType ?? enumerable.Type.GetIEnumerableElementType();
            this.enumerableValueToInit = enumerable;

            Type enumeratorType = typeof(IEnumerator<>).MakeGenericType(this.ElementType);
            this.enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
            this.currentProperty = enumeratorType.GetMethod("get_Current")!;
            this.toEnumeratorMethod = typeof(IEnumerable<>).MakeGenericType(this.ElementType).GetMethod("GetEnumerator")!;
        }

        /// <inheritdoc/>
        public Expression HasNext => this.hasNextVar;

        /// <inheritdoc/>
        public Type ElementType { get; }

        /// <inheritdoc/>
        public Expression Initialize(Expression continuation)
        {
            return Expression.Block(
                new[] { this.enumeratorVar, this.hasNextVar },
                Expression.Assign(this.enumeratorVar, Expression.Call(this.enumerableValueToInit, this.toEnumeratorMethod)),
                Expression.Assign(this.hasNextVar, Expression.Call(this.enumeratorVar, MoveNextMethod)),
                continuation);
        }

        /// <inheritdoc/>
        public Expression MoveNext(Func<ParameterExpression, Expression> continuation)
        {
            ParameterExpression currentVar = Expression.Variable(this.ElementType, "current");
            return Expression.Block(
                new[] { currentVar },
                Expression.Assign(currentVar, Expression.Property(this.enumeratorVar, this.currentProperty)),
                Expression.Assign(this.hasNextVar, Expression.Call(this.enumeratorVar, MoveNextMethod)),
                continuation(currentVar));
        }
    }
}
