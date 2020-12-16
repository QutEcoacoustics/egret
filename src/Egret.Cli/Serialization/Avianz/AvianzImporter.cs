using Egret.Cli.Models;
using Egret.Cli.Models.Avianz;
using LanguageExt;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Egret.Cli.Serialization.Avianz
{
    public class AvianzImporter : ITestCaseImporter
    {
        private readonly AvianzDeserializer avianzDeserializer;
        private readonly double defaultTolerance;
        private readonly ILogger<AvianzImporter> logger;

        public AvianzImporter(ILogger<AvianzImporter> logger, AvianzDeserializer avianzDeserializer, IOptions<AppSettings> settings)
        {
            this.logger = logger;
            this.avianzDeserializer = avianzDeserializer;
            defaultTolerance = settings.Value.DefaultThreshold;
        }

        public Option<IEnumerable<string>> CanProcess(Matcher matcher, Config config)
        {
            var result = matcher.GetResultsInFullPath(config.Location.DirectoryName);
            return result.Any() && result.All(p => Path.GetExtension(p) == ".data") ? Some(result) : None;
        }

        public async IAsyncEnumerable<TestCase> Load(IEnumerable<string> resolvedSpecifications, TestCaseInclude include, Config config, TestCaseImporter recursiveImporter)
        {
            if (include.Filter is not null)
            {
                throw new Exception("The AviaNZ importer does not currently support include filters");
            }

            foreach (var path in resolvedSpecifications)
            {
                logger.LogTrace("Loading AviaNZ data file: {file}", path);
                // we expext the test file to exist alongside the file
                // trim the ".data"  extension
                var testFile = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
                if (!File.Exists(testFile))
                {
                    throw new FileNotFoundException($"Expected source audio file to be next to AviaNZ training data file but it was not found.", testFile);
                }

                var dataFile = await avianzDeserializer.DeserializeLabelFile(path);
                var expectations = dataFile.Annotations.Select((x, i) => MakeExpectationFromAnnotation(x, i, include));

                logger.LogTrace("Data file converted to expectations: {@expectations}", expectations);

                yield return new TestCase()
                {
                    SourceInfo = dataFile.SourceInfo,
                    File = Path.GetRelativePath(Path.GetDirectoryName(path), testFile),
                    Expect = expectations.ToArray(),
                };
            }
        }

        public Expectation MakeExpectationFromAnnotation(Annotation annotation, int index, TestCaseInclude include)
        {
            var temporalTolerance = include.TemporalTolerance ?? defaultTolerance;
            var spectralTolerance = include.SpectralTolerance ?? defaultTolerance;
            // https://github.com/smarsland/AviaNZ/blob/57e6a2b43ceaaf871afa524a02c1035f0a50dd7e/Docs/file_format_specification.md#L6
            return annotation switch
            {
                { Low: 0, High: 0 } => new TemporalExpectation(annotation)
                {
                    Time = new TimeRange(annotation.Start.WithTolerance(temporalTolerance), annotation.End.WithTolerance(temporalTolerance)),
                    AnyLabel = annotation.Labels.Select(x => x.Species).ToArray(),
                    Name = $"AviaNZ Annotation {index}",
                },
                _ => new BoundedExpectation(annotation)
                {
                    Bounds = new Bounds(
                        annotation.Start.WithTolerance(temporalTolerance),
                        annotation.End.WithTolerance(temporalTolerance),
                        annotation.Low.WithTolerance(spectralTolerance),
                        annotation.High.WithTolerance(spectralTolerance)),
                    AnyLabel = annotation.Labels.Select(x => x.Species).ToArray(),
                    Name = $"AviaNZ Annotation {index}"
                }
            };
        }
    }

}