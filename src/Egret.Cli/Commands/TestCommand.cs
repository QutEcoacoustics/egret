using Egret.Cli.Extensions;
using Egret.Cli.Hosting;
using Egret.Cli.Processing;
using Egret.Cli.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Egret.Cli.Commands
{


    public class TestCommandOptions
    {
        public FileInfo Configuration { get; set; }
    }


    public class TestCommand : IEgretCommand
    {
        private readonly Executor executor;
        private readonly TestCommandOptions options;
        private readonly Deserializer serializer;
        private readonly ILogger<TestCommand> logger;
        private readonly EgretConsole console;
        private readonly ConsoleResultFormatter consoleResultFormatter;

        public TestCommand(ILogger<TestCommand> logger, EgretConsole console, Deserializer serializer, TestCommandOptions options, Executor executor, ConsoleResultFormatter consoleResultFormatter)
        {
            this.consoleResultFormatter = consoleResultFormatter;
            this.serializer = serializer;
            this.options = options;
            this.executor = executor;
            this.logger = logger;
            this.console = console;
        }
        public async Task<int> InvokeAsync(InvocationContext context)
        {

            logger.LogInformation("Test Command execute");
            console.WriteLine("Starting test command".StyleBold());
            //console.WriteLine("Using ")

            console.WriteRichLine($"Using configuration: {options.Configuration}");

            var config = serializer.Deserialize(options.Configuration);
            logger.LogTrace("config values: {@config}", config);

            var results = await executor.RunSuiteAsync(config);

            // summarize results
            console.WriteLine("Results".StyleUnderline());

            int successes = 0, failures = 0, count = 0;
            foreach (var result in results)
            {
                console.WriteLine(consoleResultFormatter.Format(count, result));
                count++;
                switch (result.Success)
                {
                    case true: successes++; break;
                    case false: failures++; break;
                }
            }

            var resultSpan = (successes / (double)results.Count).ToString("P").StyleNumber();
            console.WriteRichLine($@"
Finished. Final results:
{EgretConsole.Tab}Successes: {successes}
{EgretConsole.Tab}Failures:{failures.ToString().StyleFailure()}
{EgretConsole.Tab}Result: {resultSpan}");

            // write report

            logger.LogDebug("Received {count} results from executor", results);

            return 0;
        }


    }
}
