using Egret.Cli.Extensions;
using Egret.Cli.Formatters;
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

        public DirectoryInfo Output { get; set; } = new DirectoryInfo(Environment.CurrentDirectory);

        public bool Html { get; set; }

        public bool Json { get; set; }

        public bool Console { get; set; } = true;
    }


    public class TestCommand : IEgretCommand
    {
        private readonly Executor executor;
        private readonly TestCommandOptions options;
        private readonly Deserializer serializer;
        private readonly ILogger<TestCommand> logger;
        private readonly EgretConsole console;
        private readonly MetaFormatter resultFormatter;

        public TestCommand(ILogger<TestCommand> logger, EgretConsole console, Deserializer serializer, TestCommandOptions options, Executor executor, MetaFormatter metaFormatter
        )
        {
            this.serializer = serializer;
            this.options = options;
            this.executor = executor;
            this.logger = logger;
            this.console = console;

            this.resultFormatter = metaFormatter;
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
            await resultFormatter.WriteResultsHeader();

            int successes = 0, failures = 0, count = 0;
            foreach (var result in results)
            {
                await resultFormatter.WriteResult(count, result);
                count++;
                switch (result.Success)
                {
                    case true: successes++; break;
                    case false: failures++; break;
                }
            }

            await resultFormatter.WriteResultsFooter(count, successes, failures);

            logger.LogDebug("Received {count} results from executor", results);

            return 0;
        }


    }
}
