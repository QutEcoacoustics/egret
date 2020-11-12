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
        public static async IAsyncEnumerable<U> ForEachAsync<T, U>(
            this IEnumerable<T> source,
            int? degreesOfParallelization = default,
            [EnumeratorCancellation]
            CancellationToken token = default)
        where T : IAsyncInvokeable<U>
        {
            int index = 0;
            var max = degreesOfParallelization ?? Environment.ProcessorCount;
            var throttler = new SemaphoreSlim(max, max);


            foreach (var task in source)
            {
                await throttler.WaitAsync(token);
                try
                {
                    yield return await task.InvokeAsync(
                        Interlocked.Increment(ref index),
                        token);
                }
                finally
                {
                    throttler.Release();
                }
            }
        }
    }
}