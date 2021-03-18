using Divergic.Logging.Xunit;
using Egret.Cli;
using Egret.Cli.Processing;
using Egret.Cli.Serialization;
using Egret.Cli.Serialization.Avianz;
using Egret.Cli.Serialization.Egret;
using Egret.Cli.Serialization.Json;
using Egret.Tests.Serialization.Egret;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using Xunit.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Egret.Cli.Serialization.Audacity;
using Egret.Cli.Serialization.Xml;

namespace Egret.Tests.Support
{

    public class TestBase : IDisposable
    {
        private readonly ITestOutputHelper Output;

        public MockFileSystem TestFiles { get; }
        public List<ILogger> Loggers { get; } = new();

        public TestBase(ITestOutputHelper output)
        {
            Output = output ?? throw new ArgumentNullException(nameof(output));
            TestFiles = new MockFileSystem();
        }

        protected ILogger<T> BuildLogger<T>()
        {
            var logger = Output.BuildLoggerFor<T>(LogLevel.Trace);
            Loggers.Add(logger);
            return logger;
        }

        protected TempFactory TempFactory => new(BuildLogger<TempFactory>(), TestFiles);
        protected IOptions<AppSettings> AppSettings => Support.Helpers.DefaultAppSettings;

        protected AvianzDeserializer AvianzDeserializer => new(new DefaultJsonSerializer());

        protected AudacitySerializer AudacitySerializer => new(new DefaultXmlSerializer());

        protected ConfigDeserializer BuildConfigDeserializer()
        {
            var egret = new EgretImporter(BuildLogger<EgretImporter>(), TestFiles);
            var shared = new SharedImporter(BuildLogger<SharedImporter>(), Helpers.DefaultNamingConvention);
            var avianz = new AvianzImporter(BuildLogger<AvianzImporter>(), TestFiles, AvianzDeserializer, Helpers.DefaultAppSettings);
            var audacity = new AudacityImporter(BuildLogger<AudacityImporter>(), TestFiles, AudacitySerializer, Helpers.DefaultAppSettings);
            var importer = new TestCaseImporter(BuildLogger<TestCaseImporter>(), new ITestCaseImporter[] {
                egret, shared, avianz, audacity
            });
            return new ConfigDeserializer(
               BuildLogger<ConfigDeserializer>(),
               Helpers.DefaultAppSettings,
               importer,
               Helpers.DefaultNamingConvention,
               TestFiles);
        }

        protected string ResolvePath(string path) => TestFiles.Path.GetFullPath(path);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var logger in Loggers)
                {
                    (logger as IDisposable)?.Dispose();
                }
            }
        }
    }
}