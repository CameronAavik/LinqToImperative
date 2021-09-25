using LinqToImperative.Converters;
using LinqToImperative.ExprEnumerable;
using LinqToImperative.Expressions;
using LinqToImperative.Utils;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToImperative.QueryTree
{
    internal interface IQuery
    {
        Expression VisitQuery(ExpressionVisitor visitor);
        bool CompareWith(IQuery otherQuery, ExpressionTreeComparer comparer);
    }

    internal interface IQuery<T> : IQuery { }

    internal abstract record StreamQuery<T> : IQuery<IEnumerable<T>>
    {
        public Expression VisitQuery(ExpressionVisitor visitor) => new EnumerableExpression(AsExprEnumerable(visitor));
        public abstract IExprEnumerable AsExprEnumerable(ExpressionVisitor visitor);
        public abstract bool CompareWith(IQuery otherQuery, ExpressionTreeComparer comparer);
    }

    internal static class QueryTypes
    {
        internal sealed record ExpressionBackedStreamQuery<T>(Expression Expression) : StreamQuery<T>
        {
            private static readonly int TypeHashCode = typeof(ExpressionBackedStreamQuery<T>).GetHashCode();

            public override IExprEnumerable AsExprEnumerable(ExpressionVisitor visitor) =>
                visitor.Visit(Expression).AsExprEnumerable();

            public override bool CompareWith(IQuery otherQuery, ExpressionTreeComparer comparer) =>
                otherQuery is ExpressionBackedStreamQuery<T> otherExp
                    && comparer.Compare(Expression, otherExp.Expression);

            public override int GetHashCode() => HashHelpers.CombineHash(TypeHashCode, GetExprHash(Expression));
        }

        internal sealed record Aggregate<TSource, TAccumulate>(
            StreamQuery<TSource> Source,
            TAccumulate Seed,
            Expression<Func<TAccumulate, TSource, TAccumulate>> Func)
            : IQuery<TAccumulate>
        {
            private static readonly int TypeHashCode = typeof(Aggregate<TSource, TAccumulate>).GetHashCode();

            public Expression VisitQuery(ExpressionVisitor visitor) =>
                Source.AsExprEnumerable(visitor)
                    .Aggregate(
                        (LambdaExpression)visitor.Visit(Func),
                        visitor.Visit(Expression.Constant(Seed)));

            public bool CompareWith(IQuery otherQuery, ExpressionTreeComparer comparer) =>
                otherQuery is Aggregate<TSource, TAccumulate> otherAgg
                    && Source.CompareWith(otherAgg.Source, comparer)
                    && EqualityComparer<TAccumulate>.Default.Equals(Seed, otherAgg.Seed)
                    && comparer.Compare(Func, otherAgg.Func);

            public override int GetHashCode() =>
                HashHelpers.CombineHash(TypeHashCode, Source.GetHashCode(), Seed?.GetHashCode() ?? 0, GetExprHash(Func));
        }

        internal sealed record AggregateWithSeedExpr<TSource, TAccumulate>(
            StreamQuery<TSource> Source,
            Expression<Func<TAccumulate>> Seed,
            Expression<Func<TAccumulate, TSource, TAccumulate>> Func)
            : IQuery<TAccumulate>
        {
            private static readonly int TypeHashCode = typeof(AggregateWithSeedExpr<TSource, TAccumulate>).GetHashCode();

            public Expression VisitQuery(ExpressionVisitor visitor) => 
                Source.AsExprEnumerable(visitor)
                    .Aggregate(
                        (LambdaExpression)visitor.Visit(Func),
                        visitor.Visit(Seed.Body));

            public bool CompareWith(IQuery otherQuery, ExpressionTreeComparer comparer) =>
                otherQuery is AggregateWithSeedExpr<TSource, TAccumulate> otherAgg
                    && Source.CompareWith(otherAgg.Source, comparer)
                    && comparer.Compare(Seed, otherAgg.Seed)
                    && comparer.Compare(Func, otherAgg.Func);

            public override int GetHashCode() =>
                HashHelpers.CombineHash(TypeHashCode, Source.GetHashCode(), GetExprHash(Seed), GetExprHash(Func));
        }

        internal sealed record SelectMany<TSource, TResult>(
            StreamQuery<TSource> Source,
            Expression<Func<TSource, IEnumerable<TResult>>> Selector)
            : StreamQuery<TResult>
        {
            private static readonly int TypeHashCode = typeof(SelectMany<TSource, TResult>).GetHashCode();

            public override IExprEnumerable AsExprEnumerable(ExpressionVisitor visitor) =>
                Source.AsExprEnumerable(visitor)
                    .SelectMany((LambdaExpression)visitor.Visit(Selector));

            public override bool CompareWith(IQuery otherQuery, ExpressionTreeComparer comparer) =>
                otherQuery is SelectMany<TSource, TResult> otherSelectMany
                    && Source.CompareWith(otherSelectMany.Source, comparer)
                    && comparer.Compare(Selector, otherSelectMany.Selector);

            public override int GetHashCode() =>
                HashHelpers.CombineHash(TypeHashCode, Source.GetHashCode(), GetExprHash(Selector));
        }

        internal sealed record SelectMany<TSource, TCollection, TResult>(
            StreamQuery<TSource> Source,
            Expression<Func<TSource, IEnumerable<TCollection>>> CollectionSelector,
            Expression<Func<TSource, TCollection, TResult>> ResultSelector)
            : StreamQuery<TResult>
        {
            private static readonly int TypeHashCode = typeof(SelectMany<TSource, TCollection, TResult>).GetHashCode();

            public override IExprEnumerable AsExprEnumerable(ExpressionVisitor visitor) =>
                Source.AsExprEnumerable(visitor)
                    .SelectMany(
                        (LambdaExpression)visitor.Visit(CollectionSelector),
                        (LambdaExpression)visitor.Visit(ResultSelector));

            public override bool CompareWith(IQuery otherQuery, ExpressionTreeComparer comparer) =>
                otherQuery is SelectMany<TSource, TCollection, TResult> otherSelectMany
                    && Source.CompareWith(otherSelectMany.Source, comparer)
                    && comparer.Compare(CollectionSelector, otherSelectMany.CollectionSelector)
                    && comparer.Compare(ResultSelector, otherSelectMany.ResultSelector);

            public override int GetHashCode() =>
                HashHelpers.CombineHash(TypeHashCode, Source.GetHashCode(), GetExprHash(CollectionSelector), GetExprHash(ResultSelector));
        }

        internal sealed record Select<TSource, TResult>(
            StreamQuery<TSource> Source,
            Expression<Func<TSource, TResult>> Selector)
            : StreamQuery<TResult>
        {
            private static readonly int TypeHashCode = typeof(Select<TSource, TResult>).GetHashCode();

            public override IExprEnumerable AsExprEnumerable(ExpressionVisitor visitor) =>
                Source.AsExprEnumerable(visitor)
                    .Select((LambdaExpression)visitor.Visit(Selector));

            public override bool CompareWith(IQuery otherQuery, ExpressionTreeComparer comparer) =>
                otherQuery is Select<TSource, TResult> otherSelect
                    && Source.CompareWith(otherSelect.Source, comparer)
                    && comparer.Compare(Selector, otherSelect.Selector);

            public override int GetHashCode() => HashHelpers.CombineHash(TypeHashCode, Source.GetHashCode(), GetExprHash(Selector));
        }

        internal sealed record Where<TSource>(
            StreamQuery<TSource> Source,
            Expression<Func<TSource, bool>> Predicate)
            : StreamQuery<TSource>
        {
            private static readonly int TypeHashCode = typeof(Where<TSource>).GetHashCode();

            public override IExprEnumerable AsExprEnumerable(ExpressionVisitor visitor) =>
                Source.AsExprEnumerable(visitor)
                    .Where((LambdaExpression)visitor.Visit(Predicate));

            public override bool CompareWith(IQuery otherQuery, ExpressionTreeComparer comparer) =>
                otherQuery is Where<TSource> otherWhere
                    && Source.CompareWith(otherWhere.Source, comparer)
                    && comparer.Compare(Predicate, otherWhere.Predicate);

            public override int GetHashCode() =>
                HashHelpers.CombineHash(TypeHashCode, Source.GetHashCode(), GetExprHash(Predicate));
        }

        private static int GetExprHash(Expression expr) => ExpressionEqualityComparer.Instance.GetHashCode(expr);
    }
}
