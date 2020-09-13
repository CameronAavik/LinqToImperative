using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToImperative.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LinqToImperative.Tests
{
    [TestClass]
    public class ImperativeQueryableTests
    {
        [TestMethod]
        public void CompiledTest()
        {
            var random = new Random(42);
            var data = new int[100];
            for (int i = 0; i < 100; i++)
            {
                data[i] = random.Next(1000);
            }

            var source = new ArrayQueryableSource<int>(data);
            var executor = new QueryExecutor();
            var provider = new ImperativeQueryProvider(executor);
            var queryable = new ImperativeQueryable<int>(provider, source);

            Expression<Func<IQueryable<int>, int>> expr =
                queryable => queryable
                    .Where(i => i % 2 == 0)
                    .Aggregate(0, (acc, elem) => acc + elem);

            var substitutedExpr = expr.Substitute(new[] { queryable.Expression });
            var f = executor.Compile<int>(substitutedExpr);
            Assert.AreEqual(0, f());
        }

        [TestMethod]
        public void AggregateTest()
        {
            int[] array = Enumerable.Range(0, 100).ToArray();

            static int TestQueryableCall(IQueryable<int> q)
            {
                return q.Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);
            }

            int expected = TestQueryableCall(array.AsQueryable());
            int actual = TestQueryableCall(CreateQueryable(array));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectTest()
        {
            int[] array = Enumerable.Range(0, 100).ToArray();

            static int TestQueryableCall(IQueryable<int> q)
            {
                return q
                    .Select(i => i * 2)
                    .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);
            }

            int expected = TestQueryableCall(array.AsQueryable());
            int actual = TestQueryableCall(CreateQueryable(array));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void WhereTest()
        {
            int[] array = Enumerable.Range(0, 100).ToArray();

            static int TestQueryableCall(IQueryable<int> q)
            {
                return q
                    .Where(i => i % 2 == 0)
                    .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);
            }

            int expected = TestQueryableCall(array.AsQueryable());
            int actual = TestQueryableCall(CreateQueryable(array));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectManyArrayTest()
        {
            int[][] array2d = Enumerable.Range(0, 5).Select(i => Enumerable.Range(i, 5).ToArray()).ToArray();

            static int TestQueryableCall(IQueryable<int[]> q)
            {
                return q
                    .SelectMany(i => i)
                    .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);
            }

            int expected = TestQueryableCall(array2d.AsQueryable());
            int actual = TestQueryableCall(CreateQueryable(array2d));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectManyEnumerableTest()
        {
            int[][] array2d = Enumerable.Range(0, 5).Select(i => Enumerable.Range(i, 5).ToArray()).ToArray();

            static int TestQueryableCall(IQueryable<int[]> q)
            {
                return q
                    .SelectMany(i => i.Select(i => i + 1))
                    .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);
            }

            int expected = TestQueryableCall(array2d.AsQueryable());
            int actual = TestQueryableCall(CreateQueryable(array2d));
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ComplexTest()
        {
            int[][] array2d = Enumerable.Range(0, 100).Select(i => Enumerable.Range(i, 100).ToArray()).ToArray();

            static int TestQueryableCall(IQueryable<int[]> q)
            {
                return q
                    .SelectMany(i => i)
                    .Where(i => i % 3 == 0)
                    .Select(i => i * 4)
                    .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);
            }

            int expected = TestQueryableCall(array2d.AsQueryable());
            int actual = TestQueryableCall(CreateQueryable(array2d));
            Assert.AreEqual(expected, actual);
        }

        private static ImperativeQueryable<T> CreateQueryable<T>(T[] arr)
        {
            var source = new ArrayQueryableSource<T>(arr);
            var executor = new QueryExecutor();
            var provider = new ImperativeQueryProvider(executor);
            return new ImperativeQueryable<T>(provider, source);
        }
    }
}
