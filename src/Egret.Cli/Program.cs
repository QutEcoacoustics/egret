using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Egret.Cli.Commands;
using System.Reflection;
using System.CommandLine.Binding;
using Egret.Cli.Extensions;
using System.CommandLine.Rendering;
using Serilog;
using Egret.Cli.Serialization;
using Serilog.Configuration;

namespace Egret.Cli
{
    class Program
    {
        public static readonly Argument<FileInfo> configArg =
            new Argument<FileInfo>(nameof(TestCommandOptions.Configuration), "The config file to look for")
            .ExistingOnly();


        static async Task<int> Main(string[] args)
        {


            return await BuildCommandLine()
                // https://github.com/dotnet/command-line-api/blob/main/src/System.CommandLine.Hosting/HostingExtensions.cs
                .UseHost(Host.CreateDefaultBuilder, ConfigureAppHost)
                // https://github.com/dotnet/command-line-api/blob/main/src/System.CommandLine/Builder/CommandLineBuilderExtensions.cs#L257
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }

        private static CommandLineBuilder BuildCommandLine()
        {


            var rootCommand = new RootCommand("Egret")
            {
                new Command("version", description: "get the version of egret"),
                new Command("test", "Run egret tests")
                {
                    configArg
                },
                new Command("watch", "Runs egret tests every time a change is found")
                {
                    configArg,
                    new Option<bool>("--poll", "Use polling instead of filesystem events").WithAlias("-p")
                }
            };

            rootCommand.Handler = CommandHandler.Create<IHost>(Run);



            var builder = new CommandLineBuilder(rootCommand);


            return builder;
        }


        private static int Run(IHost host)
        {
            var log = host.Services.GetRequiredService<ILogger<Program>>();
            log.LogDebug("Run main");

            return 0;
        }

        private static void ConfigureAppHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ConsoleRenderer>(provider =>
                {
                    return new ConsoleRenderer(
                        provider.GetRequiredService<IConsole>(),
                        OutputMode.Ansi
                    );
                });
                services.AddSingleton<ITerminal>((p) =>
                {
                    return p.GetRequiredService<IConsole>().GetTerminal(true);
                });

                services.AddSingleton<Serializer>();
            })
            .UseEgretCommand<TestCommandOptions, TestCommand>("test")
            .UseEgretCommand<WatchCommandOptions, WatchCommand>("watch")
            .UseSerilog((hostingContext, services, loggerConfiguration) =>
            {

                loggerConfiguration
                    .Enrich.FromLogContext()
                    .MinimumLevel.Debug()
                    .WriteTo.Console();
            });
        }


    }
}
