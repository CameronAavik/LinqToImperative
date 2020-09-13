using System.Linq.Expressions;

namespace LinqToImperative.Internal
{
    /// <summary>
    /// Class containing the implementation of Where for <see cref="IExprEnumerable"/> objects.
    /// </summary>
    public static partial class ExprEnumerableExtensions
    {
        /// <summary>
        /// Takes an enumerable of element T, and an expression from T -> bool, and filters the
        /// enumerable to elements where the predicate is true.
        /// </summary>
        /// <param name="enumerable">The enumerable to filter.</param>
        /// <param name="predicate">The predicate to filter on.</param>
        /// <returns>The filtered enumerable.</returns>
        internal static IExprEnumerable Where(this IExprEnumerable enumerable, LambdaExpression predicate) =>
            enumerable.SelectRaw(
                (cur, k) => Expression.IfThen(predicate.Substitute(cur), k(cur)),
                enumerable.ElementType);
    }
}
