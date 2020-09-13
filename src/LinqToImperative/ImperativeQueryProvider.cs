using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToImperative.Internal;

namespace LinqToImperative
{
    /// <summary>
    /// The implementation of <see cref="IQueryProvider"/> that handles creation and execution of the
    /// <see cref="ImperativeQueryable{T}"/> objects.
    /// </summary>
    public class ImperativeQueryProvider : IQueryProvider
    {
        private static readonly MethodInfo GenericCreateQueryMethod
            = typeof(ImperativeQueryProvider).GetRuntimeMethods()
                .Single(m => m.Name == nameof(CreateQuery) && m.IsGenericMethod);

        private static readonly MethodInfo GenericExecuteMethod
            = typeof(ImperativeQueryProvider).GetRuntimeMethods()
                .Single(m => m.Name == nameof(Execute) && m.IsGenericMethod);

        private readonly IQueryExecutor queryExecutor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImperativeQueryProvider"/> class.
        /// </summary>
        /// <param name="queryExecutor">The query executor.</param>
        public ImperativeQueryProvider(IQueryExecutor queryExecutor)
        {
            this.queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
        }

        /// <inheritdoc/>
        public IQueryable CreateQuery(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            // Extract T from the first IEnumerable<T> interface that is implemented on the expression's type.
            Type elementType = expression.Type.GetIEnumerableElementType();

            return GenericCreateQueryMethod
                .MakeGenericMethod(elementType)
                .Invoke(this, new object[] { expression }) as IQueryable
                ?? throw new SystemException("CreateQuery method did not return an IQueryable");
        }

        /// <inheritdoc/>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new ImperativeQueryable<TElement>(this, expression);

        /// <inheritdoc/>
        public object? Execute(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return GenericExecuteMethod.MakeGenericMethod(expression.Type).Invoke(this, new object[] { expression });
        }

        /// <inheritdoc/>
        public TResult Execute<TResult>(Expression expression) =>
            this.queryExecutor.Execute<TResult>(expression);
    }
}
