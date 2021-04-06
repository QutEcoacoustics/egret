using Egret.Cli.Extensions;
using Egret.Cli.Formatters;
using Egret.Cli.Hosting;
using Egret.Cli.Models;
using Egret.Cli.Models.Results;
using Egret.Cli.Processing;
using Egret.Cli.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Egret.Cli.Commands
{


    public class TestCommandOptions
    {
        public FileInfo Configuration { get; set; }

        public DirectoryInfo Output { get; set; } = new DirectoryInfo(Environment.CurrentDirectory);

        public bool Html { get; set; } = false;

        public bool Json { get; set; } = false;

        public bool NoConsole { get; set; } = false;

        public bool Csv { get; set; } = false;

        public bool Audacity { get; set; } = false;

        public bool Sequential { get; set; } = false;
    }


    public class TestCommand : IEgretCommand
    {
        private readonly Executor executor;
        private readonly TestCommandOptions options;
        private readonly ConfigDeserializer serializer;
        private readonly ILogger<TestCommand> logger;
        private readonly EgretConsole console;
        private readonly MetaFormatter resultFormatter;

        public TestCommand(ILogger<TestCommand> logger, EgretConsole console, ConfigDeserializer serializer, TestCommandOptions options, Executor executor, MetaFormatter metaFormatter
        )
        {
            this.serializer = serializer;
            this.options = options;
            this.executor = executor;
            this.logger = logger;
            this.console = console;

            resultFormatter = metaFormatter;
        }
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var timeTaken = Stopwatch.StartNew();
            logger.LogInformation("Test Command execute");
            console.WriteLine("Starting test command".StyleBold());
            //console.WriteLine("Using ")

            console.WriteRichLine($"Using configuration: {options.Configuration}");

            var config = await LoadConfig();
            if (config.IsNone)
            {
                return ExitCodes.ConfigurationFailure;
            }

            await console.CreateProgressBar("Running tests");
            var progress = new Progress<double>((p) => console.ReportProgress(p));

            var results = await executor.RunAllSuitesAsync((Config)config, progress, options.Sequential ? 1 : Environment.ProcessorCount);

            await console.DestroyProgressBar();

            // summarize results
            await resultFormatter.WriteResultsHeader();

            var resultStats = new ResultsStatistics();
            foreach (var result in results)
            {
                resultStats.ProcessRecord(result);
                await resultFormatter.WriteResult(resultStats.TotalResults, result);
            }


            timeTaken.Stop();
            await resultFormatter.WriteResultsFooter(new FinalResults(
                (Config)config,
                resultStats,
                timeTaken.Elapsed
            ));

            //logger.LogDebug("Received {count} results from executor", results);

            return resultStats.TotalFailures is 0 ? ExitCodes.Success : ExitCodes.TestFailure;
        }

        private async ValueTask<LanguageExt.Option<Config>> LoadConfig()
        {
            var (config, errors) = await serializer.Deserialize((FileInfoBase)options.Configuration);

            logger.LogTrace("config values: {@config}", config);
            if (errors.Any())
            {
                logger.LogError("Config file loading errors: {@errors}", errors);
                console.WriteLine(
                    new ContainerSpan(
                        "Error: there was a problem loading the config file:".StyleFailure(),
                        new ContainerSpan(
                            errors
                                .SelectMany(x => new[] {
                                    EgretConsole.NewLine,
                                    EgretConsole.SoftTab,
                                    x.Message.StyleFailure()
                                 })
                                .ToArray()
                        )
                    )
                );
                return None;
            }

            return config;
        }


    }
}
