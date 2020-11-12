using Microsoft.Extensions.Logging;
using System;

namespace Egret.Cli.Commands
{
    public class GlobalOptions
    {
        public LogLevel LogLevel { get; set; } = LogLevel.None;
        public bool Verbose { get; set; }
        public bool VeryVerbose { get; set; }

        public LogLevel FinalLogLevel()
        {
            var flagLevel = (Verbose, VeryVerbose) switch
            {
                (true, _) => LogLevel.Debug,
                (_, true) => LogLevel.Trace,
                _ => LogLevel.None
            };

            return (LogLevel)Math.Min(
                (int)LogLevel,
                (int)flagLevel
            );
        }
    }
}