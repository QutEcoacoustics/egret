using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Egret.Cli.Commands;
using Egret.Cli.Extensions;
using Egret.Cli.Hosting;
using System.Text;

namespace Egret.Cli
{
    class Program
    {
        public static readonly Argument<FileInfo> configArg =
            new Argument<FileInfo>(nameof(TestCommandOptions.Configuration), "The config file to look for")
            .ExistingOnly();


        static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            return await BuildCommandLine()
                // https://github.com/dotnet/command-line-api/blob/main/src/System.CommandLine.Hosting/HostingExtensions.cs
                .UseHost(Host.CreateDefaultBuilder, HostingSetup.ConfigureAppHost)
                // https://github.com/dotnet/command-line-api/blob/main/src/System.CommandLine/Builder/CommandLineBuilderExtensions.cs#L257
                .UseDefaults()
                //.UseAnsiTerminalWhenAvailable()
                .Build()
                .InvokeAsync(args);
        }

        private static CommandLineBuilder BuildCommandLine()
        {


            var rootCommand = new RootCommand("Egret")
            {
                // commands
                new Command("version", description: "get the version of egret"),
                new Command("test", "Run egret tests")
                {
                    configArg,
                    new Option<DirectoryInfo>("--output", description: "Set the directory to write reports to").WithAlias("-o"),
                    new Option<bool>("--json", description: "Output results to a json file").WithAlias("-j"),
                    new Option<bool>("--console", description: "Output results in the console").WithAlias("-c"),
                    new Option<bool>("--html", description: "Output results to a HTML file").WithAlias("-h"),
                    new Option<bool>("--sequential", description: "Disable parallel execution").WithAlias("-s"),
                },
                new Command("watch", "Runs egret tests every time a change is found")
                {
                    configArg,
                    new Option<bool>("--poll", "Use polling instead of filesystem events").WithAlias("-p")
                }
            };

            // global options
            rootCommand.AddGlobalOption(new Option<LogLevel>("--log-level", "Enable logging and the log level").WithAlias("-l"));
            rootCommand.AddGlobalOption(new Option<bool>("--verbose", "Enable logging at the debug level").WithAlias("-v"));
            rootCommand.AddGlobalOption(new Option<bool>("--very-verbose", "Enable logging at the trace level"));



            rootCommand.Handler = MainCommand.RunHandler;

            var builder = new CommandLineBuilder(rootCommand);


            return builder;
        }
    }
}
