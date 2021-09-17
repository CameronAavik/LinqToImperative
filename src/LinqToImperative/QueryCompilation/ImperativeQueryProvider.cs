using LinqToImperative.Utils;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToImperative.QueryCompilation
{
    /// <summary>
    /// The implementation of <see cref="IQueryProvider"/> that handles creation and execution of the
    /// <see cref="ImperativeQueryable{T}"/> objects.
    /// </summary>
    public class ImperativeQueryProvider : IQueryProvider
    {
        private static readonly MethodInfo GenericCreateQueryMethod =
            ReflectionUtils.GetGenericMethod<ImperativeQueryProvider>(qp => qp.CreateQuery<int>(default!));

        private static readonly MethodInfo GenericExecuteMethod =
            ReflectionUtils.GetGenericMethod<ImperativeQueryProvider>(qp => qp.Execute<int>(default!));

        private readonly IQueryCompiler queryCompiler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImperativeQueryProvider"/> class.
        /// </summary>
        /// <param name="queryCompiler">The query compiler.</param>
        internal ImperativeQueryProvider(IQueryCompiler queryCompiler)
        {
            this.queryCompiler = queryCompiler ?? throw new ArgumentNullException(nameof(queryCompiler));
        }

        /// <inheritdoc/>
        public IQueryable CreateQuery(Expression expression) =>
            (IQueryable)GenericCreateQueryMethod
                .MakeGenericMethod((Type)expression.Type.GetIEnumerableElementType())
                .Invoke(this, new object[] { expression })!;

        /// <inheritdoc/>
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new ImperativeQueryable<TElement>(this, expression);

        /// <inheritdoc/>
        public object? Execute(Expression expression) =>
            GenericExecuteMethod
                .MakeGenericMethod(expression.Type)
                .Invoke(this, new object[] { expression });

        /// <inheritdoc/>
        public TResult Execute<TResult>(Expression expression) =>
            queryCompiler.Compile(Expression.Lambda<Func<TResult>>(expression, null)).Invoke();
    }
}
