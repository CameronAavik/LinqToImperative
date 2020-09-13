using System;
using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// An <see cref="IExprEnumerable"/> that represents how to enumerate over an array
    /// in an imperative way.
    /// </summary>
    internal class ArrayExprEnumerable : ILinearExprEnumerable
    {
        private readonly Expression? arrValueToInit;
        private readonly ParameterExpression arrVar;
        private readonly ParameterExpression indexVar = Expression.Variable(typeof(int), "i");
        private readonly ParameterExpression lenVar = Expression.Variable(typeof(int), "len");

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayExprEnumerable"/> class.
        /// </summary>
        /// <param name="arr">An expression representing the array.</param>
        /// <param name="elementType">The type of the array's elements if available.</param>
        public ArrayExprEnumerable(Expression arr, Type? elementType = null)
        {
            if (arr is null)
            {
                throw new ArgumentNullException(nameof(arr));
            }

            this.ElementType = elementType ?? arr.Type.GetIEnumerableElementType();

            if (arr is ParameterExpression param)
            {
                this.arrVar = param;
            }
            else
            {
                this.arrVar = Expression.Variable(this.ElementType.MakeArrayType(), "arr");
                this.arrValueToInit = arr;
            }
        }

        /// <inheritdoc/>
        public Type ElementType { get; }

        /// <inheritdoc/>
        public Expression HasNext => Expression.LessThan(this.indexVar, this.lenVar); // i < len

        /// <inheritdoc/>
        public Expression Initialize(Expression continuation)
        {
            ParameterExpression[] paramArray =
                this.arrValueToInit == null
                    ? new[] { this.indexVar, this.lenVar }
                    : new[] { this.arrVar, this.indexVar, this.lenVar };

            Expression assignLen = Expression.Assign(this.lenVar, Expression.ArrayLength(this.arrVar));
            Expression assignIndex = Expression.Assign(this.indexVar, Expression.Constant(0));

            Expression[] expressions =
                this.arrValueToInit == null
                    ? new[] { assignLen, assignIndex, continuation }
                    : new[] { Expression.Assign(this.arrVar, this.arrValueToInit), assignLen, assignIndex, continuation };

            return Expression.Block(paramArray, expressions);
        }

        /// <inheritdoc/>
        public Expression MoveNext(Func<ParameterExpression, Expression> continuation)
        {
            /*
             * elem = arr[i];
             * i++;
             */

            ParameterExpression elemVar = Expression.Variable(this.ElementType, "elem");
            return Expression.Block(
                new[] { elemVar },
                Expression.Assign(elemVar, Expression.ArrayIndex(this.arrVar, this.indexVar)),
                Expression.PostIncrementAssign(this.indexVar),
                continuation(elemVar));
        }
    }
}
