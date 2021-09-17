using LinqToImperative.ExprEnumerable;
using LinqToImperative.Producers;

namespace LinqToImperative.Converters
{
    /// <summary>
    /// Extensions for <see cref="IProducer"/>
    /// </summary>
    public static partial class ProducerExtensions
    {
        /// <summary>
        /// Converts an IProducer into an ExprEnumerable
        /// </summary>
        /// <param name="producer"></param>
        /// <returns></returns>
        internal static IExprEnumerable AsExprEnumerable(this IProducer producer) => new LinearExprEnumerable(producer);
    }
}
