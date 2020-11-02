using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
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


        public WatchCommandOptions Options { get; }

        public WatchCommand(ILogger<TestCommand> logger, WatchCommandOptions options)
        {
            this.Options = options;
            Logger = logger;
        }

        public ILogger<TestCommand> Logger { get; }

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            Logger.LogInformation("Watch Command execute");
            Logger.LogDebug("options: {options}", this.Options);
            Logger.LogDebug("file: {file}", this.Options.Configuration);
            Logger.LogDebug("poll?: {poll}", this.Options.Poll);
            return await Task.FromResult(0);
        }


    }
}
