using LinqToImperative.Producers;
using LinqToImperative.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToImperative.Converters.Producers
{
    /// <summary>
    /// A producer that produces elements from an IEnumerable.
    /// </summary>
    internal readonly struct EnumerableProducer : IProducer
    {
        private static readonly MethodInfo MoveNextMethod = ReflectionUtils.GetMethod<IEnumerator>(x => x.MoveNext());

        private readonly Expression enumerable;
        private readonly ParameterExpression enumeratorVar;
        private readonly ParameterExpression hasNextVar;

        /// <summary>
        /// Creates an instance of <see cref="EnumerableProducer"/>.
        /// </summary>
        /// <param name="enumerable">An expression representing the enumerable.</param>
        /// <param name="elementType">The type of the array element.</param>
        public EnumerableProducer(Expression enumerable, Type? elementType = null)
        {
            this.enumerable = enumerable;
            ElementType = elementType ?? enumerable.Type.GetIEnumerableElementType();
            enumeratorVar = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(ElementType), "enumerator");
            hasNextVar = Expression.Variable(typeof(bool), "hasNext");
        }

        /// <inheritdoc/>
        public Type ElementType { get; }

        /// <inheritdoc/>
        public Expression Initialize(Expression continuation)
        {
            MethodInfo toEnumeratorMethod = typeof(IEnumerable<>).MakeGenericType(ElementType).GetMethod("GetEnumerator")!;
            return Expression.Block(
                new[] { enumeratorVar, hasNextVar },
                Expression.Assign(enumeratorVar, Expression.Call(enumerable, toEnumeratorMethod)),
                Expression.Assign(hasNextVar, Expression.Call(enumeratorVar, MoveNextMethod)),
                continuation);
        }

        /// <inheritdoc/>
        public Expression HasNext => hasNextVar;

        /// <inheritdoc/>
        public Expression MoveNext(Func<ParameterExpression, Expression> continuation)
        {
            MethodInfo currentProperty = enumeratorVar.Type.GetMethod("get_Current")!;
            ParameterExpression currentVar = Expression.Variable(ElementType, "current");
            return Expression.Block(
                new[] { currentVar },
                Expression.Assign(currentVar, Expression.Property(enumeratorVar, currentProperty)),
                Expression.Assign(hasNextVar, Expression.Call(enumeratorVar, MoveNextMethod)),
                continuation(currentVar));
        }


        /// <inheritdoc/>
        public IProducer VisitChildren(ExpressionVisitor visitor)
        {
            var newEnumerable = visitor.Visit(enumerable);
            return enumerable == newEnumerable ? this : new EnumerableProducer(newEnumerable, ElementType);
        }
    }
}
