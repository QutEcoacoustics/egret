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
using Microsoft.Extensions.Configuration;
using Egret.Cli.Processing;
using Serilog.Sinks.SystemConsole.Themes;
using System.CommandLine.Help;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System.Net.Http;
using System.Collections.Generic;
using System.Collections.Immutable;
using Egret.Cli.Formatters;
using Egret.Cli.Serialization.Avianz;
using Egret.Cli.Serialization.Json;

namespace Egret.Cli.Hosting
{
    public static class HostingSetup
    {
        internal static void ConfigureAppHost(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(configBuilder =>
            {
                configBuilder.AddEnvironmentVariables("EGRET");
            })
            .ConfigureServices(ConfigureServices)
            .UseEgretGlobalOptions<GlobalOptions>()
            .UseEgretCommand<TestCommandOptions, TestCommand>("test")
            .UseEgretCommand<WatchCommandOptions, WatchCommand>("watch")
            .UseSerilog(ConfigureSerilog);
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddOptions<AppSettings>().BindCommandLine();


            // Use console rendere to render "updating screens"
            // services.AddSingleton<ConsoleRenderer>(provider =>
            // {
            //     return new ConsoleRenderer(
            //         provider.GetRequiredService<IConsole>(),
            //         OutputMode.Auto
            //     );
            // });
            services.AddSingleton<EgretConsole>();
            services.AddSingleton<HttpClient>();
            services.AddSingleton<ConfigDeserializer>();
            services.AddSingleton<DefaultJsonSerializer>();
            services.AddSingleton<LiterateSerializer>();
            services.AddSingleton<AvianzDeserializer>();

            services.AddTransient<TempFactory>();
            services.AddTransient<Executor>();
            services.AddTransient<CaseExecutor>();
            services.AddSingleton<CaseExecutorFactory>();

            // tool runner uses temp factory, which we want to be uniquely created for each use
            // thus we can't allow tool runner to be a singleton
            services.AddTransient<ToolRunner>();

            services.AddSingleton<ConsoleResultFormatter>();
            services.AddSingleton<JsonResultFormatter>();
            services.AddSingleton<HtmlResultFormatter>();
            services.AddSingleton<MetaFormatter>();

            services.AddSingleton<SharedImporter>();
            services.AddSingleton<AvianzImporter>();
            services.AddSingleton((provider) => new ITestCaseImporter[] {
                provider.GetRequiredService<SharedImporter>(),
                provider.GetRequiredService<AvianzImporter>(),
            });
            services.AddSingleton<TestCaseImporter>();
        }

        private static LogEventLevel GetLogLevel(IServiceProvider provider)
        {
            return LevelConvert.ToSerilogLevel(provider.GetRequiredService<GlobalOptions>().FinalLogLevel());
        }

        private static readonly string FilterEgretConsoleName = typeof(EgretConsole).FullName;

        private static void ConfigureSerilog(
            HostBuilderContext hostingContext,
            IServiceProvider services,
            LoggerConfiguration loggerConfiguration)
        {
            loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .Destructure.ToMaximumDepth(20)
                .Destructure.With(new EgretSerilogDestructuringPolicy())
                .MinimumLevel.Is(GetLogLevel(services))

                .WriteTo.Console(
                    theme: SerilogTheme.Custom,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} <{ThreadId,2}>]{Scope} {Message}{NewLine}{Exception}"
                )
                .Filter.ByExcluding(logEvent =>
                    logEvent.Properties.TryGetValue("SourceContext", out var source)
                    && (source as ScalarValue).Value.Equals(FilterEgretConsoleName)
                );
        }

    }
}