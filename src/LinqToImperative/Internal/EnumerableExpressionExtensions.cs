using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// Class containing the implementation of Select for <see cref="EnumerableExpression"/> objects.
    /// </summary>
    public static class EnumerableExpressionExtensions
    {
        private static readonly MethodInfo MoveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;

        /// <summary>
        /// Creates an enumerable expression that wraps an IEnumerable.
        /// </summary>
        /// <param name="enumerable">The IEnumerable to wrap.</param>
        /// <param name="enumerableType">The type of the elements of the IEnumerable.</param>
        /// <returns>An enumerable expression for the array.</returns>
        public static EnumerableExpression OfEnumerable(Expression enumerable, Type? enumerableType = null)
        {
            if (enumerable is null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            Type elementType = enumerableType ?? enumerable.Type.GetIEnumerableElementType();
            Type enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

            MethodInfo currentProperty = enumeratorType.GetMethod("get_Current")!;
            MethodInfo toEnumeratorMethod = typeof(IEnumerable<>).MakeGenericType(elementType).GetMethod("GetEnumerator")!;

            ParameterExpression enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
            ParameterExpression hasNextVar = Expression.Variable(typeof(bool), "hasNext");

            Expression Initialize(Expression continuation)
            {
                return Expression.Block(
                    new[] { enumeratorVar, hasNextVar },
                    Expression.Assign(enumeratorVar, Expression.Call(enumerable, toEnumeratorMethod)),
                    Expression.Assign(hasNextVar, Expression.Call(enumeratorVar, MoveNextMethod)),
                    continuation);
            }

            Expression MoveNext(Func<ParameterExpression, Expression> continuation)
            {
                ParameterExpression currentVar = Expression.Variable(elementType, "current");
                return Expression.Block(
                    new[] { currentVar },
                    Expression.Assign(currentVar, Expression.Property(enumeratorVar, currentProperty)),
                    Expression.Assign(hasNextVar, Expression.Call(enumeratorVar, MoveNextMethod)),
                    continuation(currentVar));
            }

            return new LinearEnumerableExpression(Initialize, hasNextVar, MoveNext, elementType);
        }

        /// <summary>
        /// Creates an enumerable expression that wraps an array.
        /// </summary>
        /// <param name="arr">The array to wrap.</param>
        /// <param name="arrType">The type of the elements of the array.</param>
        /// <returns>An enumerable expression for the array.</returns>
        public static EnumerableExpression OfArray(Expression arr, Type? arrType = null)
        {
            if (arr is null)
            {
                throw new ArgumentNullException(nameof(arr));
            }

            Type elementType = arrType ?? arr.Type.GetElementType()!;
            ParameterExpression indexVar = Expression.Variable(typeof(int), "i");
            ParameterExpression lenVar = Expression.Variable(typeof(int), "len");

            bool needsToAssignArr = false;
            if (arr is not ParameterExpression arrVar)
            {
                arrVar = Expression.Variable(arr.Type, "arr");
                needsToAssignArr = true;
            }

            Expression Intialize(Expression continuation)
            {
                Expression assignLen = Expression.Assign(lenVar, Expression.ArrayLength(arrVar));
                Expression assignIndex = Expression.Assign(indexVar, Expression.Constant(0));

                if (needsToAssignArr)
                {
                    return Expression.Block(
                        new[] { arrVar, indexVar, lenVar },
                        Expression.Assign(arrVar, arr),
                        assignLen,
                        assignIndex,
                        continuation);
                }
                else
                {
                    return Expression.Block(
                        new[] { indexVar, lenVar },
                        assignLen,
                        assignIndex,
                        continuation);
                }
            }

            Expression hasNext = Expression.LessThan(indexVar, lenVar);

            Expression MoveNext(Func<ParameterExpression, Expression> continuation)
            {
                ParameterExpression elemVar = Expression.Variable(elementType, "elem");
                return Expression.Block(
                    new[] { elemVar },
                    Expression.Assign(elemVar, Expression.ArrayIndex(arrVar, indexVar)),
                    Expression.PostIncrementAssign(indexVar),
                    continuation(elemVar));
            }

            return new LinearEnumerableExpression(Intialize, hasNext, MoveNext, elementType);
        }

        /// <summary>
        /// Takes a lambda expression that maps the elements of the enumerable and returns an
        /// enumerable of the elements after they were mapped.
        /// </summary>
        /// <param name="expression">The enumerable expression.</param>
        /// <param name="selector">The mapping function.</param>
        /// <returns>The enumerable after mapping.</returns>
        public static EnumerableExpression Select(this EnumerableExpression expression, LambdaExpression selector)
        {
            Func<ParameterExpression, Expression> SelectorBody(Func<ParameterExpression, Expression> continuation)
            {
                Expression Inner(ParameterExpression curElem)
                {
                    ParameterExpression newElemVar = Expression.Variable(selector.ReturnType, "newElem");
                    return Expression.Block(
                        new[] { newElemVar },
                        Expression.Assign(newElemVar, selector.Substitute(curElem)),
                        continuation(newElemVar));
                }

                return Inner;
            }

            return expression.SelectRaw(SelectorBody, selector.ReturnType);
        }

        /// <summary>
        /// Takes an enumerable of element T, and an expression from T -> bool, and filters the
        /// enumerable to elements where the predicate is true.
        /// </summary>
        /// <param name="expression">The enumerable to filter.</param>
        /// <param name="predicate">The predicate to filter on.</param>
        /// <returns>The filtered enumerable.</returns>
        public static EnumerableExpression Where(this EnumerableExpression expression, LambdaExpression predicate)
        {
            return expression.SelectRaw(
                k => cur => Expression.IfThen(predicate.Substitute(cur), k(cur)),
                expression.ElementType);
        }

        /// <summary>
        /// Takes two expressions, func (S -> T -> S) and seed (S), and an enumerable with elements
        /// of type T, and aggregates (aka folds) the elements to produce a valid of type S.
        /// </summary>
        /// <param name="expression">The enumerable to aggregate.</param>
        /// <param name="func">The aggregator function.</param>
        /// <param name="seed">The initial state used when aggregating.</param>
        /// <returns>An expression representing the calculation of the aggregation.</returns>
        public static Expression Aggregate(this EnumerableExpression expression, LambdaExpression func, Expression seed)
        {
            ParameterExpression accVar = Expression.Variable(seed.Type, "acc");
            return Expression.Block(
                new[] { accVar },
                Expression.Assign(accVar, seed),
                expression.AggregateRaw(e => Expression.Assign(accVar, func.Substitute(accVar, e))),
                accVar);
        }

        /// <summary>
        /// Takes an IEnumerable{T}, a lambda of T -> IEnumerable{U} and returns an IEnumerable{U}.
        /// This is also known as a flatmap.
        /// </summary>
        /// <param name="enumerable">The original enumerable to flatmap.</param>
        /// <param name="selector">The flatmapper function.</param>
        /// <returns>The flatmapped enumerable.</returns>
        public static EnumerableExpression SelectMany(this EnumerableExpression enumerable, LambdaExpression selector)
        {
            Type newType = selector.ReturnType.GetIEnumerableElementType();
            EnumerableExpression ToEnumerableExpresion(Expression expr)
            {
                return expr.Type.IsArray
                    ? OfArray(expr, newType)
                    : OfEnumerable(expr, newType);
            }

            return enumerable.SelectManyRaw(elem => ToEnumerableExpresion(selector.Substitute(elem)), newType);
        }

        /// <summary>
        /// Takes a selector and uses it to map the elements of an enumerable.
        /// expression : Expression{IQueryable{T}}.
        /// selector : ((Expression{U} -> Expression{void}) -> (Expression{T} -> Expression{void})).
        /// returns : Expression{IQueryable{U}}.
        ///
        /// The selector takes a continuation that handles new elements and returns a continuation
        /// that takes old elements, gets the new element, and passes it into that continuation.
        /// </summary>
        /// <param name="expression">The enumerable expression to map.</param>
        /// <param name="selector">The mapping function.</param>
        /// <param name="newElementType">The new type of the enumerable.</param>
        /// <returns>The new mapped enumerable.</returns>
        private static EnumerableExpression SelectRaw(
            this EnumerableExpression expression,
            Func<Func<ParameterExpression, Expression>, Func<ParameterExpression, Expression>> selector,
            Type newElementType)
        {
            return expression switch
            {
                LinearEnumerableExpression linear =>
                    new LinearEnumerableExpression(
                        linear.Initialize,
                        linear.HasNext,
                        c => linear.MoveNext(selector(c)),
                        newElementType),
                NestedEnumerableExpression nested =>
                    new NestedEnumerableExpression(
                        nested.BaseEnumerable,
                        e => nested.GetNestedEnumerable(e).SelectRaw(selector, newElementType),
                        newElementType),
                _ => throw new ArgumentException("Must be a linear or nested enumerable.", nameof(expression)),
            };
        }

        /// <summary>
        /// Takes an aggregator function and uses it to aggregate the elements of the enumerable.
        /// The aggregator function takes a parameter of type T and returns a void expression.
        /// This aggregator function can be thought of like the body of a for loop.
        /// </summary>
        /// <param name="expression">The enumerable to aggregate.</param>
        /// <param name="func">The aggregator function.</param>
        /// <returns>An expression that contains the calculation of the aggregation.</returns>
        private static Expression AggregateRaw(this EnumerableExpression expression, Func<ParameterExpression, Expression> func)
        {
            Expression GenLinearAggregate(LinearEnumerableExpression linear)
            {
                LabelTarget label = Expression.Label();

                Expression loopExpr =
                    Expression.Loop(
                        Expression.IfThenElse(
                            linear.HasNext,
                            linear.MoveNext(func),
                            Expression.Break(label)),
                        label);

                return linear.Initialize(loopExpr);
            }

            return expression switch
            {
                LinearEnumerableExpression linear => GenLinearAggregate(linear),
                NestedEnumerableExpression nested =>
                    nested.BaseEnumerable.AggregateRaw(e => nested.GetNestedEnumerable(e).AggregateRaw(func)),
                _ => throw new ArgumentException("Must be a linear or nested enumerable.", nameof(expression)),
            };
        }

        /// <summary>
        /// Takes a selector and uses it to flatmap the elements of an enumerable.
        /// </summary>
        /// <param name="enumerable">Enumerable to flatmap.</param>
        /// <param name="selector">The flatmapping function.</param>
        /// <param name="newType">The new element type.</param>
        /// <returns>The flatmapped enumerable.</returns>
        private static EnumerableExpression SelectManyRaw(this EnumerableExpression enumerable, Func<ParameterExpression, EnumerableExpression> selector, Type newType)
        {
            return enumerable switch
            {
                LinearEnumerableExpression linear => new NestedEnumerableExpression(linear, selector, newType),
                NestedEnumerableExpression nested => new NestedEnumerableExpression(
                    nested.BaseEnumerable,
                    e => nested.GetNestedEnumerable(e).SelectManyRaw(selector, newType),
                    newType),
                _ => throw new Exception("ExprEnumerable is in an invalid state, must be a linear or nested enumerable."),
            };
        }
    }
}
