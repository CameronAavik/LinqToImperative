using System;
using System.Linq.Expressions;

namespace LinqToImperative.Producers
{
    /// <summary>
    /// A producer defines how a linear sequence of values is to be produced.
    /// </summary>
    internal interface IProducer
    {
        /// <summary>
        /// The type of elements produced by this producer.
        /// </summary>
        public Type ElementType { get; }

        /// <summary>
        /// Returns an expression which initializes all the variables needed for the producer to run.
        /// The expression must also call the continuation at the end of initialization.
        /// </summary>
        /// <param name="continuation">Expression representing the work to be completed after initialization.</param>
        /// <returns>The initialization expression.</returns>
        public Expression Initialize(Expression continuation);

        /// <summary>
        /// Gets an expression of type bool that returns true if there are more elements.
        /// </summary>
        public Expression HasNext { get; }

        /// <summary>
        /// Returns an expression which steps the producer forwards.
        /// The expression must then also call the continuation which is retrieved by passing in a parameter representing the current value.
        /// </summary>
        /// <param name="continuation">A function that takes in an expression</param>
        /// <returns></returns>
        public Expression MoveNext(Func<ParameterExpression, Expression> continuation);

        /// <summary>
        /// Visit any child expressions in the producer.
        /// </summary>
        /// <param name="visitor">The expression visitor.</param>
        /// <returns>The visited producer.</returns>
        public IProducer VisitChildren(ExpressionVisitor visitor);
    }
}
