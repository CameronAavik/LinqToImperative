using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;
using LinqToImperative.Internal;

namespace LinqToImperative.Benchmarks
{
    //[EtwProfiler]
    public class TestBenchmark
    {
        [Params(10, 100, 1000, 10000, 100000)]
        public int N { get; set; }

        private int[] data;

        private static readonly Func<int[], int> compiledQueryableCall =
            ImperativeQueryableExtensions.Compile<int[], int>(
                data => data
                    .AsImperativeQueryable()
                    .Where(i => i % 2 == 0)
                    .Aggregate(0, (acc, elem) => acc + elem));

        private static readonly Func<int[], int> compiledHandWrittenExpression =
            SumEvensHandWrittenExpressionImpl().Compile();

        [GlobalSetup]
        public void GlobalSetup()
        {
            var random = new Random(1234);
            this.data = new int[this.N];
            for (int i = 0; i < this.N; i++)
            {
                this.data[i] = random.Next(1000);
            }
        }

        [Benchmark]
        public int SumEvensHandWritten()
        {
            return SumEvensHandWrittenImpl(this.data);
        }

        [Benchmark]
        public int SumEvensCompiledHandWrittenExpression()
        {
            return compiledHandWrittenExpression(this.data);
        }

        [Benchmark]
        public int SumEvensCompiledImperativeQueryable()
        {
            return compiledQueryableCall(this.data);
        }

        [Benchmark]
        public int SumEventsHandWrittenExpression()
        {
            return SumEvensHandWrittenExpressionImpl().Compile()(this.data);
        }

        [Benchmark]
        public int SumEvensImperativeQueryable()
        {
            return this.data
                .AsImperativeQueryable()
                .Where(i => i % 2 == 0)
                .Aggregate(0, (acc, elem) => acc + elem);
        }

        [Benchmark]
        public int SumEvensEnumerable()
        {
            return this.data
                .Where(i => i % 2 == 0)
                .Aggregate(0, (acc, elem) => acc + elem);
        }

        [Benchmark]
        public int SumEvensQueryable()
        {
            return this.data
                .AsQueryable()
                .Where(i => i % 2 == 0)
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
                                        Expression.Modulo(elemVar, Expression.Constant(2)),
                                        Expression.Constant(0)),
                                    Expression.Assign(sumVar, Expression.Add(sumVar, elemVar)))),
                            Expression.Break(breakLabel)),
                        breakLabel),
                    sumVar),
                dataVar);
        }

        private static int SumEvensHandWrittenImpl(int[] data)
        {
            int sum = 0;
            int length = data.Length;
            for (int i = 0; i < length; i++)
            {
                int val = data[i];
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
            _ = BenchmarkRunner.Run<TestBenchmark>(
#if DEBUG
                    new DebugInProcessConfig()
#endif
                );

        }
    }
}
