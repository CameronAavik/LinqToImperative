using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;
using LinqToImperative.Converters;

namespace LinqToImperative.Benchmarks
{
    //[EtwProfiler]
    public class TestBenchmark
    {
        [Params(10/*, 100, 1000, 10000, 100000, 1000000, 10000000*/)]
        public int N { get; set; }

        private int[] data;

        private static readonly Expression<Func<int[], int>> queryableCall = data =>
            data
                .AsImperativeQueryable()
                .Where(i => (i & 1) == 0)
                .Aggregate(0, (acc, elem) => acc + elem);

        private static readonly Func<int[], int> compiledQueryableCall = LinqToImperative.CompileQuery(queryableCall);
        private static readonly Func<int[], int> compiledHandWrittenExpression = SumEvensHandWrittenExpressionImpl().Compile();


        [GlobalSetup]
        public void GlobalSetup()
        {
            var random = new Random(1234);
            data = new int[N];
            for (int i = 0; i < N; i++)
            {
                data[i] = random.Next(1000);
            }
        }

        //[Benchmark]
        public int SumEvensHandWritten()
        {
            int sum = 0;
            int length = data.Length;
            for (int i = 0; i < length; i++)
            {
                int val = data[i];
                if ((val & 1) == 0)
                {
                    sum += val;
                }
            }

            return sum;
        }

        [Benchmark]
        public int SumEvensImperativeQueryable()
        {

            return data
                .AsImperativeQueryable()
                .Where(i => (i & 1) == 0)
                .Aggregate(0, (acc, elem) => acc + elem);
        }

        private static readonly Expression<Func<int, bool>> wherePredicate = i => (i & 1) == 0;
        private static readonly Expression<Func<int, int, int>> aggregateFunc = (acc, elem) => acc + elem;

        [Benchmark]
        public int SumEvensImperativeQueryableCachedExpressions()
        {

            return data
                .AsImperativeQueryable()
                .Where(wherePredicate)
                .Aggregate(0, aggregateFunc);
        }

        //[Benchmark]
        public int SumEvensEnumerable()
        {
            return data
                .Where(i => (i & 1) == 0)
                .Aggregate(0, (acc, elem) => acc + elem);
        }

        //[Benchmark]
        public int SumEvensCompiledHandWrittenExpression()
        {
            return compiledHandWrittenExpression(data);
        }

        //[Benchmark]
        public int SumEvensCompiledImperativeQueryable()
        {
            return compiledQueryableCall(data);
        }

        //[Benchmark]
        public int SumEventsHandWrittenExpression()
        {
            return SumEvensHandWrittenExpressionImpl().Compile()(data);
        }

        //[Benchmark]
        public int SumEvensQueryable()
        {
            return data
                .AsQueryable()
                .Where(i => (i & 1) == 0)
                .Aggregate(0, (acc, elem) => acc + elem);
        }

        private static Expression<Func<int[], int>> SumEvensHandWrittenExpressionImpl()
        {
            var dataVar = Expression.Variable(typeof(int[]), "data");
            var sumVar = Expression.Variable(typeof(int), "sum");
            var iVar = Expression.Variable(typeof(int), "i");
            var lenVar = Expression.Variable(typeof(int), "len");
            var elemVar = Expression.Variable(typeof(int), "elem");
            var breakLabel = Expression.Label();

            return Expression.Lambda<Func<int[], int>>(
                Expression.Block(
                    new[] { sumVar, iVar, lenVar },
                    Expression.Assign(sumVar, Expression.Constant(0)),
                    Expression.Assign(lenVar, Expression.ArrayLength(dataVar)),
                    Expression.Assign(iVar, Expression.Constant(0)),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(iVar, lenVar),
                            Expression.Block(
                                new[] { elemVar },
                                Expression.Assign(elemVar, Expression.ArrayIndex(dataVar, iVar)),
                                Expression.PostIncrementAssign(iVar),
                                Expression.IfThen(
                                    Expression.Equal(
                                        Expression.And(elemVar, Expression.Constant(1)),
                                        Expression.Constant(0)),
                                    Expression.Assign(sumVar, Expression.Add(sumVar, elemVar)))),
                            Expression.Break(breakLabel)),
                        breakLabel),
                    sumVar),
                dataVar);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            _ = BenchmarkRunner.Run<TestBenchmark>(
#if DEBUG
                    new DebugInProcessConfig()
#endif
                );

        }
    }
}
