using Egret.Cli.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Diagnostics;

namespace Egret.Cli.Extensions
{
    public static class ILoggerExtensions
    {
        public static MeasureStopWatch<T> Measure<T>(this ILogger<T> logger, string name)
        {
            return new MeasureStopWatch<T>(logger, name);
        }

        public sealed class MeasureStopWatch<T> : IDisposable
        {
            private readonly Stopwatch stopWatch;
            private readonly ILogger<T> logger;
            private readonly string name;

            public MeasureStopWatch(ILogger<T> logger, string name)
            {
                this.name = name;

                this.stopWatch = Stopwatch.StartNew();
                this.logger = logger;
                //this.logger.BeginScope(this);
            }

            public void Dispose()
            {
                this.stopWatch.Stop();
                this.logger.LogInformation("{name} took {time}", name, stopWatch.Elapsed);
            }

            public TimeSpan Stop()
            {
                this.stopWatch.Stop();
                return this.stopWatch.Elapsed;
            }
        }
    }
}