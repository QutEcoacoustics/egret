using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Egret.Cli.Extensions
{
    public interface IAsyncInvokeable<TResult>
    {
        Task<TResult> InvokeAsync(int index, CancellationToken token);
    }
    public static class EnumerableExtensions
    {
        //public delegate
        /// <summary>
        ///     Executes a foreach asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="degreesOfParallelization">The degrees of parallelism.</param>
        /// <param name="body">The body.</param>
        /// <remarks> 
        /// https://github.com/dotnet/runtime/issues/1946
        /// /// </remarks>
        /// <returns></returns>
        public static async Task<IList<U>> ForEachAsync<T, U>(
            this IEnumerable<T> source,
            Action<int, U> completed = null,
            int? degreesOfParallelization = default,
            CancellationToken token = default)
        where T : IAsyncInvokeable<U>
        {
            int index = 0;
            var max = degreesOfParallelization ?? Environment.ProcessorCount;
            var throttler = new SemaphoreSlim(max);

            var jobs = new ConcurrentBag<Task<U>>();
            foreach (var task in source)
            {
                int thisIndex = Interlocked.Increment(ref index);

                var jobTask = Task
                    .Run(
                        async () =>
                        {
                            try
                            {
                                await throttler.WaitAsync(token);
                                var result = await task.InvokeAsync(thisIndex, token);
                                completed?.Invoke(thisIndex, result);
                                return result;
                            }
                            finally
                            {
                                throttler.Release();
                            }
                        },
                        token
                    );
                jobs.Add(jobTask);

            }

            var result = await Task.WhenAll(jobs);

            return result;
        }

        public static IEnumerable<T> One<T>(this T item)
        {
            yield return item;
        }

        public static IEnumerable<T> Subsample<T>(this IEnumerable<T> items, int every)
        {
            if (every < 1) { throw new ArgumentException("every must be greater than or equal to 1", nameof(every)); }
            var counter = 0;
            foreach (var item in items)
            {
                if (counter % every == 0)
                {
                    yield return item;
                }
                counter++;
            }
        }

        public static IEnumerable<T> Subsample<T>(this IEnumerable<T> items, Option<long> seed = default, double skew = 0.5)
        {
            if (skew is > 1 or < 0) { throw new ArgumentException("skew must be in [0,1]", nameof(skew)); }

            var random = new Random((int)seed.IfNone(Environment.TickCount));

            foreach (var item in items)
            {
                if (random.NextDouble() >= skew)
                {
                    yield return item;
                }
            }
        }
    }
}