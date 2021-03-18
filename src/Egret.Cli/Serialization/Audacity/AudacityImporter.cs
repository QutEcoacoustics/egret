namespace Egret.Cli.Serialization.Audacity
{
    using LanguageExt;
    using LanguageExt.Common;
    using Microsoft.Extensions.Logging;
    using Models;
    using Processing;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using Microsoft.Extensions.Options;
    using Models.Expectations;
    using System;
    using System.IO;
    using static LanguageExt.Prelude;

    public class AudacityImporter : ITestCaseImporter
    {
        private readonly ILogger<AudacityImporter> logger;
        private readonly IFileSystem fileSystem;
        private readonly AudacitySerializer serializer;
        private readonly double defaultTolerance;

        private const string ProjectFileExtension = ".aup";

        public AudacityImporter(ILogger<AudacityImporter> logger, IFileSystem fileSystem, AudacitySerializer serializer,
            IOptions<AppSettings> settings)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.serializer = serializer;
            this.defaultTolerance = settings.Value.DefaultThreshold;
        }

        public Validation<Error, Option<IEnumerable<string>>> CanProcess(string matcher, Config config)
        {
            (IEnumerable<Error> errors, IEnumerable<string> results) = PathResolver
                .ResolvePathOrGlob(fileSystem, matcher, config.Location.DirectoryName)
                .Partition();

            IEnumerable<Error> errorList = errors.ToList();

            if (errorList.Any())
            {
                return errorList.ToSeq();
            }

            IEnumerable<string> resultList = results.ToList();
            return resultList.Any() && resultList.All(p => Path.GetExtension(p) == ProjectFileExtension)
                ? Some(resultList)
                : None;
        }

        public async IAsyncEnumerable<TestCase> Load(
            IEnumerable<string> resolvedSpecifications,
            ImporterContext context)
        {
            if (context.Include.Filter is not null)
            {
                throw new Exception("The Audacity importer does not currently support include filters");
            }

            foreach (var path in resolvedSpecifications)
            {
                logger.LogTrace("Loading Audacity data file: {file}", path);

                await using Stream stream = fileSystem.File.OpenRead(path);
                var dataFile = serializer.Deserialize(stream, path);

                var expectations = dataFile?.Tracks?.SelectMany((track, trackIndex) =>
                        track.Labels.Select((label, labelIndex) =>
                        {
                            var temporalTolerance = context.Include.TemporalTolerance ?? defaultTolerance;
                            var spectralTolerance = context.Include.SpectralTolerance ?? defaultTolerance;
                            var overrideBounds = context.Include.Override;

                            var name =
                                $"Audacity Label {label.Title} in track {track.Name} ({trackIndex}:{labelIndex})";

                            return label.IsSelPoint
                                ? (IExpectation)new TemporalExpectation(label)
                                {
                                    Time = new TimeRange(
                                        label.TimeStart.WithTolerance(temporalTolerance),
                                        label.TimeEnd.WithTolerance(temporalTolerance)),
                                    AnyLabel = new[] {label.Title},
                                    Name = name,
                                }
                                : (IExpectation)new BoundedExpectation(label)
                                {
                                    Bounds = new Bounds(
                                        overrideBounds?.Start ?? label.TimeStart.WithTolerance(temporalTolerance),
                                        overrideBounds?.End ?? label.TimeEnd.WithTolerance(temporalTolerance),
                                        overrideBounds?.Low ?? label.SelLow.WithTolerance(spectralTolerance),
                                        overrideBounds?.High ?? label.SelHigh.WithTolerance(spectralTolerance)),
                                    AnyLabel = new[] {label.Title},
                                    Name = name,
                                };
                        }))
                    .ToList() ?? new List<IExpectation> {new NoEvents()};

                if (expectations.Count == 1 && expectations[0] is NoEvents)
                {
                    logger.LogWarning("No annotations found in {path}, producing a no events expectation", path);
                }

                if (context.Include.Exhaustive.HasValue && context.Include.Exhaustive.Value)
                {
                    expectations.Add(new NoExtraResultsExpectation() {Match = context.Include.Exhaustive.Value});
                }

                logger.LogTrace("Data file converted to expectations: {@expectations}", expectations);

                yield return new TestCase {SourceInfo = dataFile?.SourceInfo, Expect = expectations.ToArray(),};
            }
        }
    }
}