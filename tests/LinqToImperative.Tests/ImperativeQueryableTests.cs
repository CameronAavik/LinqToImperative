using System;
using System.Linq;
using LinqToImperative.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LinqToImperative.Tests
{
    [TestClass]
    public class ImperativeQueryableTests
    {
        [TestMethod]
        public void CompiledTest1()
        {
            var random = new Random(42);
            var data = new int[100];
            for (int i = 0; i < 100; i++)
            {
                data[i] = random.Next(1000);
            }

            var compiledQuery = LinqToImperative.CompileQuery
                (() => data
                    .AsImperativeQueryable()
                    .Where(i => i % 2 == 0)
                    .Aggregate(0, (acc, elem) => acc + elem));

            var expected = data
                .Where(i => i % 2 == 0)
                .Aggregate(0, (acc, elem) => acc + elem);

            var actual = compiledQuery();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CompiledTest2()
        {
            var random = new Random(42);
            var data = new int[100];
            for (int i = 0; i < 100; i++)
            {
                data[i] = random.Next(1000);
            }

            var compiledQuery = LinqToImperative.CompileQuery<int[], int>
                (data => data
                    .AsImperativeQueryable()
                    .Where(i => i % 2 == 0)
                    .Aggregate(0, (acc, elem) => acc + elem));

            var expected = data
                .Where(i => i % 2 == 0)
                .Aggregate(0, (acc, elem) => acc + elem);

            var actual = compiledQuery(data);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void AggregateTest()
        {
            int[] array = Enumerable.Range(0, 100).ToArray();

            int expected = array
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            int actual = array
                .AsImperativeQueryable()
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectTest()
        {
            int[] array = Enumerable.Range(0, 100).ToArray();

            int expected = array
                .Select(i => i * 2)
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            int actual = array
                .AsImperativeQueryable()
                .Select(i => i * 2)
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void WhereTest()
        {
            int[] array = Enumerable.Range(0, 100).ToArray();

            int expected = array
                .Where(i => i % 2 == 0)
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            int actual = array
                .AsImperativeQueryable()
                .Where(i => i % 2 == 0)
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectManyArrayTest()
        {
            int[][] array2d = Enumerable.Range(0, 5).Select(i => Enumerable.Range(i, 5).ToArray()).ToArray();

            int expected = array2d
                .SelectMany(i => i)
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            int actual = array2d
                .AsImperativeQueryable()
                .SelectMany(i => i)
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SelectManyEnumerableTest()
        {
            int[][] array2d = Enumerable.Range(0, 5).Select(i => Enumerable.Range(i, 5).ToArray()).ToArray();

            int expected = array2d
                .SelectMany(i => i.Select(i => i + 1))
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            int actual = array2d
                .AsImperativeQueryable()
                .SelectMany(i => i.Select(i => i + 1))
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ComplexTest()
        {
            int[][] array2d = Enumerable.Range(0, 100).Select(i => Enumerable.Range(i, 100).ToArray()).ToArray();

            int expected = array2d
                .SelectMany(i => i)
                .Where(i => i % 3 == 0)
                .Select(i => i * 4)
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            int actual = array2d
                .AsImperativeQueryable()
                .SelectMany(i => i)
                .Where(i => i % 3 == 0)
                .Select(i => i * 4)
                .Aggregate(13, (acc, elem) => (acc + elem * 27) % 100001);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ComplexTestWithClosureVariables()
        {
            int[][] array2d = Enumerable.Range(0, 100).Select(i => Enumerable.Range(i, 100).ToArray()).ToArray();

            int x = 13;
            int y = 27;
            int z = 1000001;

            int expected = array2d
                .SelectMany(i => i)
                .Where(i => i % 3 == 0)
                .Select(i => i * 4)
                .Aggregate(x, (acc, elem) => (acc + elem * y) % z);

            int actual = array2d
                .AsImperativeQueryable()
                .SelectMany(i => i)
                .Where(i => i % 3 == 0)
                .Select(i => i * 4)
                .Aggregate(x, (acc, elem) => (acc + elem * y) % z);

            Assert.AreEqual(expected, actual);
        }
    }
}
