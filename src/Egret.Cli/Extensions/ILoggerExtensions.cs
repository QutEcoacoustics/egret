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

        public static U PassThrough<T, U>(this ILogger<T> logger, in U value, string message, LogLevel logLevel = LogLevel.Information)
        {
            logger.Log(logLevel, message, value);
            return value;
        }

        public sealed class MeasureStopWatch<T> : IDisposable
        {
            private readonly Stopwatch stopWatch;
            private readonly ILogger<T> logger;
            private readonly string name;

            public MeasureStopWatch(ILogger<T> logger, string name)
            {
                this.name = name;

                stopWatch = Stopwatch.StartNew();
                this.logger = logger;
            }

            public void Dispose()
            {
                stopWatch.Stop();
                logger.LogInformation("{name} took {time}", name, stopWatch.Elapsed);
            }

            public TimeSpan Stop()
            {
                stopWatch.Stop();
                return stopWatch.Elapsed;
            }
        }
    }
}