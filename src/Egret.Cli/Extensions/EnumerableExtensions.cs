using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
    }
}