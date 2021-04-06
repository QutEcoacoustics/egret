using System;
using System.CommandLine.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Egret.Cli.Commands;
using Egret.Cli.Extensions;
using Serilog;
using Egret.Cli.Serialization;
using Microsoft.Extensions.Configuration;
using Egret.Cli.Processing;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System.Net.Http;
using Egret.Cli.Formatters;
using Egret.Cli.Serialization.Avianz;
using Egret.Cli.Serialization.Json;
using YamlDotNet.Serialization.NamingConventions;
using Egret.Cli.Serialization.Egret;
using System.IO.Abstractions;
using Egret.Cli.Serialization.Audacity;

namespace Egret.Cli.Hosting
{
    using Serialization.Xml;

    public class HostingSetup
    {
        private static readonly string FilterEgretConsoleName = typeof(EgretConsole).FullName;
        private readonly RunInfo runInfo;

        public HostingSetup(DateTime startedAt)
        {
            runInfo = new RunInfo(startedAt);
        }

        internal void ConfigureAppHost(IHostBuilder builder)
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

        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddOptions<AppSettings>().BindCommandLine();

            services.AddSingleton<RunInfo>(runInfo);
            services.AddSingleton<OutputFile>();

            // Use console renderer to render "updating screens"
            // services.AddSingleton<ConsoleRenderer>(provider =>
            // {
            //     return new ConsoleRenderer(
            //         provider.GetRequiredService<IConsole>(),
            //         OutputMode.Auto
            //     );
            // });
            services.AddSingleton<EgretConsole>();
            services.AddSingleton<HttpClient>();
            services.AddSingleton<IFileSystem>(_ => new FileSystem());
            services.AddSingleton(_ => UnderscoredNamingConvention.Instance);
            services.AddSingleton<ConfigDeserializer>();
            services.AddSingleton<DefaultJsonSerializer>();
            services.AddSingleton<DefaultXmlSerializer>();
            services.AddSingleton<LiterateSerializer>();
            services.AddSingleton<AvianzDeserializer>();
            services.AddSingleton<AudacitySerializer>();
            services.AddSingleton<Audacity3Serializer>();

            services.AddTransient<TempFactory>();
            services.AddTransient<Executor>();
            services.AddTransient<CaseExecutor>();
            services.AddSingleton<CaseExecutorFactory>();

            // tool runner uses temp factory, which we want to be uniquely created for each use
            // thus we can't allow tool runner to be a singleton
            services.AddTransient<ToolRunner>();

            services.AddSingleton<ConsoleResultFormatter>();
            services.AddSingleton<JsonResultFormatter>();
            services.AddSingleton<CsvResultFormatter>();
            services.AddSingleton<HtmlResultFormatter>();
            services.AddSingleton<MetaFormatter>();
            services.AddSingleton<AudacityResultFormatter>();

            services.AddSingleton<SharedImporter>();
            services.AddSingleton<AvianzImporter>();
            services.AddSingleton<AudacityImporter>();
            services.AddSingleton<EgretImporter>();
            services.AddSingleton((provider) => new ITestCaseImporter[] {
                provider.GetRequiredService<SharedImporter>(),
                provider.GetRequiredService<AvianzImporter>(),
                provider.GetRequiredService<EgretImporter>(),
                provider.GetRequiredService<AudacityImporter>(),
            });
            services.AddSingleton<TestCaseImporter>();
        }

        private static LogEventLevel GetLogLevel(IServiceProvider provider)
        {
            return LevelConvert.ToSerilogLevel(provider.GetRequiredService<GlobalOptions>().FinalLogLevel());
        }

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

    public record RunInfo(DateTime StartedAt);
}