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

            var f = ImperativeQueryableExtensions.Compile<int[], int>
                (data => data
                    .AsImperativeQueryable()
                    .Where(i => i % 2 == 0)
                    .Aggregate(0, (acc, elem) => acc + elem));

            var expected = data.Where(i => i % 2 == 0).Aggregate(0, (acc, elem) => acc + elem);

            Assert.AreEqual(expected, f(data));
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
            int actual = TestQueryableCall(array.AsImperativeQueryable());
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
            int actual = TestQueryableCall(array.AsImperativeQueryable());
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
            int actual = TestQueryableCall(array.AsImperativeQueryable());
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
            int actual = TestQueryableCall(array2d.AsImperativeQueryable());
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
            int actual = TestQueryableCall(array2d.AsImperativeQueryable());
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
            int actual = TestQueryableCall(array2d.AsImperativeQueryable());
            Assert.AreEqual(expected, actual);
        }
    }
}
