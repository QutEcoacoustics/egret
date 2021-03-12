using System;
using System.Threading;

namespace Egret.Cli
{
    public class AppSettings
    {
        public double DefaultThreshold { get; set; } = 0.5;

        public TimeSpan ToolTimeout { get; set; } = TimeSpan.FromSeconds(60.0);
    }
}