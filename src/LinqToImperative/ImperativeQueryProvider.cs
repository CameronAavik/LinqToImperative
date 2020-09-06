using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        private readonly IQueryExecutor _queryExecutor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImperativeQueryProvider"/> class.
        /// </summary>
        /// <param name="queryExecutor">The query executor.</param>
        public ImperativeQueryProvider(IQueryExecutor queryExecutor)
        {
            _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
        }

        /// <inheritdoc/>
        public IQueryable CreateQuery(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            // Extract T from the first IEnumerable<T> interface that is implemented on the expression's type.
            Type elementType = GetIEnumerableElementType(expression.Type);

            return GenericCreateQueryMethod
                .MakeGenericMethod(elementType)
                .Invoke(this, new object[] { expression }) as IQueryable
                ?? throw new SystemException("CreateQuery method did not return an IQueryable");
        }

        /// <inheritdoc/>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new ImperativeQueryable<TElement>(this, expression);
        }

        /// <inheritdoc/>
        public object? Execute(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return GenericExecuteMethod
                .MakeGenericMethod(expression.Type)
                .Invoke(this, new object[] { expression });
        }

        /// <inheritdoc/>
        public TResult Execute<TResult>(Expression expression)
        {
            return _queryExecutor.Execute<TResult>(expression);
        }

        /// <summary>
        /// Returns the T from the first <see cref="IEnumerable{T}"/> interface that is implemented by the provided type.
        /// </summary>
        /// <param name="type">The type to get the element type from.</param>
        /// <returns>The element type.</returns>
        private static Type GetIEnumerableElementType(Type type)
        {
            Type typeDefinitionToFind = typeof(IEnumerable<>);
            foreach (var implementedInterface in type.GetInterfaces())
            {
                if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == typeDefinitionToFind)
                {
                    return implementedInterface.GenericTypeArguments[0];
                }
            }

            throw new ArgumentException("The type does not implement IEnumerable<T>", nameof(type));
        }
    }
}
