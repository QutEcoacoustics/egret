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
            services.AddSingleton<Deserializer>();
            services.AddSingleton<LiterateSerializer>();

            services.AddTransient<TempFactory>();
            services.AddTransient<Executor>();
            services.AddTransient<CaseExecutor>();
            services.AddSingleton<CaseExecutorFactory>();
            services.AddSingleton<ToolRunner>();

            services.AddSingleton<ConsoleResultFormatter>();
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
                .MinimumLevel.Is(GetLogLevel(services))
                .WriteTo.Console(
                    theme: SerilogTheme.Custom,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} <{ThreadId,2}>]{Scope} {Message}{NewLine}{Exception}"
                ).Filter.ByExcluding(logEvent =>
                    logEvent.Properties.TryGetValue("SourceContext", out var source)
                    && (source as ScalarValue).Value.Equals(FilterEgretConsoleName)
                );
        }

    }
}