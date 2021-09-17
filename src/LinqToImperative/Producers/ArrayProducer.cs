using LinqToImperative.Expressions;
using LinqToImperative.Producers;
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqToImperative.Converters.Producers
{
    /// <summary>
    /// A producer that produces elements from an array.
    /// </summary>
    internal readonly struct ArrayProducer : IProducer
    {
        private readonly Expression arr;
        private readonly ParameterExpression indexVar;
        private readonly ParameterExpression lenVar;
        private readonly ParameterExpression arrVar;

        /// <summary>
        /// Creates an instance of <see cref="ArrayProducer"/>.
        /// </summary>
        /// <param name="arr">An expression representing the array.</param>
        /// <param name="elementType">The type of the array element.</param>
        public ArrayProducer(Expression arr, Type? elementType = null)
        {
            this.arr = arr;
            ElementType = elementType ?? arr.Type.GetElementType()!;
            indexVar = Expression.Variable(typeof(int), "i");
            lenVar = Expression.Variable(typeof(int), "len");
            arrVar = Expression.Variable(arr.Type, "arr");
        }

        /// <inheritdoc/>
        public Type ElementType { get; }

        /// <inheritdoc/>
        public Expression Initialize(Expression continuation) =>
            Expression.Block(
                new ReadOnlyCollectionBuilder<ParameterExpression>(3) { arrVar, indexVar, lenVar },
                new ReadOnlyCollectionBuilder<Expression>(4)
                {
                    Expression.Assign(arrVar, arr),
                    Expression.Assign(lenVar, Expression.ArrayLength(arrVar)),
                    Expression.Assign(indexVar, Expression.Constant(0)),
                    continuation
                });

        /// <inheritdoc/>
        public Expression HasNext => Expression.LessThan(indexVar, lenVar);

        /// <inheritdoc/>
        public Expression MoveNext(Func<ParameterExpression, Expression> continuation)
        {
            ParameterExpression elemVar = Expression.Variable(ElementType, "elem");
            return Expression.Block(
                new ReadOnlyCollectionBuilder<ParameterExpression>(1) { elemVar },
                new ReadOnlyCollectionBuilder<Expression>(3)
                {
                    Expression.Assign(elemVar, Expression.ArrayIndex(arrVar, indexVar)),
                    Expression.PostIncrementAssign(indexVar),
                    continuation(elemVar)
                });
        }

        /// <summary>
        /// Creates a <see cref="ArrayProducer"/> from an array.
        /// </summary>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <param name="arr">The source array.</param>
        /// <returns>The array producer.</returns>
        public static ArrayProducer Create<T>(T[] arr)
        {
            var arrExpr = new EnumerableSourceExpression(arr);
            return new ArrayProducer(arrExpr, typeof(T));
        }
    }
}
