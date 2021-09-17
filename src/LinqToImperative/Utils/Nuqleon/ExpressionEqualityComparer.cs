// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToImperative.Utils.Nuqleon
{
    /// <summary>
    /// Customizable equality comparer for expression trees. Default behavior matches trees in a structural fashion.
    /// </summary>
    public class ExpressionEqualityComparer : IEqualityComparer<Expression>
    {
        private readonly Func<ExpressionEqualityComparator> _comparatorFactory;

        /// <summary>
        /// Creates a new expression equality comparer with structural matching behavior.
        /// </summary>
        public ExpressionEqualityComparer()
        {
            _comparatorFactory = () => new ExpressionEqualityComparator();
        }

        /// <summary>
        /// Creates a new expression equality comparer with custom matching behavior implemented on the specified comparator.
        /// </summary>
        /// <param name="comparatorFactory">Factory for comparators that define custom matching behavior.</param>
        public ExpressionEqualityComparer(Func<ExpressionEqualityComparator> comparatorFactory)
        {
            _comparatorFactory = comparatorFactory ?? throw new ArgumentNullException(nameof(comparatorFactory));
        }

        /// <summary>
        /// Checks whether the two given expressions are equal.
        /// </summary>
        /// <param name="x">First expression.</param>
        /// <param name="y">Second expression.</param>
        /// <returns>true if both expressions are equal; otherwise, false.</returns>
        public bool Equals(Expression? x, Expression? y) => GetComparator().Equals(x, y);

        /// <summary>
        /// Gets a hash code for the given expression.
        /// </summary>
        /// <param name="obj">Expression to compute a hash code for.</param>
        /// <returns>Hash code for the given expression.</returns>
        public int GetHashCode(Expression obj) => GetComparator().GetHashCode(obj);

        private ExpressionEqualityComparator GetComparator()
        {
            var comparator = _comparatorFactory();

            if (comparator == null)
            {
                throw new InvalidOperationException("Factory returned null reference.");
            }

            return comparator;
        }
    }
}
