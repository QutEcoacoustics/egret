using Egret.Cli.Extensions;
using Egret.Cli.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Egret.Cli.Commands
{

    public class WatchCommandOptions
    {
        public FileInfo Configuration { get; set; }


        public bool Poll { get; set; }
    }


    public class WatchCommand : IEgretCommand
    {
        private readonly EgretConsole console;

        public WatchCommandOptions Options { get; }

        public WatchCommand(ILogger<TestCommand> logger, WatchCommandOptions options, EgretConsole console)
        {
            this.Options = options;
            this.console = console;
            Logger = logger;
        }

        public ILogger<TestCommand> Logger { get; }

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            Logger.LogInformation("Watch Command execute");
            Logger.LogDebug("options: {options}", Options);
            Logger.LogDebug("file: {file}", Options.Configuration);
            Logger.LogDebug("poll?: {poll}", Options.Poll);
            Console.WriteLine(TaskScheduler.Current.MaximumConcurrencyLevel);
            console.WriteLine("Test".StyleUnderline());
            await console.CreateProgressBar("Engage");
            var random = new Random();
            int counter = 0;
            for (double p = 0; p <= 1.0; p += 0.01)
            {
                if (random.NextDouble() > 0.9)
                {
                    console.WriteLine($"Random status message! {counter.ToString().StyleValue()}");
                    counter++;
                }
                console.ReportProgress(p);
                Thread.Sleep(1);
            }

            console.WriteLine("before progress bar destroy".StyleUnimportant());
            await console.DestroyProgressBar();
            console.WriteLine("afterwards".StyleSuccess());


            return await Task.FromResult(0);
        }


    }
}
