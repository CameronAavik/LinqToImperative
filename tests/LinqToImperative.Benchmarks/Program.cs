using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LinqToImperative.Internal;

namespace LinqToImperative.Benchmarks
{
    public class TestBenchmark
    {
        [Params(/*10, 100, 1000, 10000, */100000)]
        public int N { get; set; }

        private int[] data;

        private Func<int> compiledQueryableCall;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var random = new Random(42);
            this.data = new int[this.N];
            for (int i = 0; i < this.N; i++)
            {
                this.data[i] = random.Next(1000);
            }

            var source = new ArrayQueryableSource<int>(this.data);
            var executor = new QueryExecutor();
            var provider = new ImperativeQueryProvider(executor);
            var queryable = new ImperativeQueryable<int>(provider, source);

            Expression<Func<IQueryable<int>, int>> expr =
                queryable => queryable
                    .Where(i => i % 2 == 0)
                    .Aggregate(0, (acc, elem) => acc + elem);

            var substitutedExpr = expr.Substitute(new[] { queryable.Expression });
            this.compiledQueryableCall = executor.Compile<int>(substitutedExpr);
        }

        [Benchmark]
        public int SumEvensImperativeQueryable()
        {
            var source = new ArrayQueryableSource<int>(this.data);
            var executor = new QueryExecutor();
            var provider = new ImperativeQueryProvider(executor);
            var queryable = new ImperativeQueryable<int>(provider, source);

            return queryable
                .Where(i => i % 2 == 0)
                .Aggregate(0, (acc, elem) => acc + elem);
        }

        [Benchmark]
        public int SumEvensDefaultQueryable()
        {
            return this.data
                .AsQueryable()
                .Where(i => i % 2 == 0)
                .Aggregate(0, (acc, elem) => acc + elem);
        }

        [Benchmark]
        public int SumEvensCompiledImperativeQueryable()
        {
            return this.compiledQueryableCall();
        }

        [Benchmark]
        public int SumEvensHandWritten()
        {
            int sum = 0;
            int length = this.data.Length;
            for (int i = 0; i < length; i++)
            {
                int val = this.data[i];
                if (val % 2 == 0)
                {
                    sum += val;
                }
            }

            return sum;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TestBenchmark>();
        }
    }
}
