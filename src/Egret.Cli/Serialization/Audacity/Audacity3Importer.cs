namespace Egret.Cli.Serialization.Audacity
{
    using LanguageExt;
    using LanguageExt.Common;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;
    using Models.Audacity;
    using Models.Expectations;
    using Processing;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using static LanguageExt.Prelude;

    public class Audacity3Importer : ITestCaseImporter
    {
        private const string ProjectFileExtension = ".aup3";
        private readonly double defaultTolerance;
        private readonly IFileSystem fileSystem;
        private readonly ILogger<Audacity3Importer> logger;
        private readonly Audacity3Serializer serializer;

        public Audacity3Importer(ILogger<Audacity3Importer> logger, IFileSystem fileSystem, Audacity3Serializer serializer,
            IOptions<AppSettings> settings)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.serializer = serializer;
            defaultTolerance = settings.Value.DefaultThreshold;
        }
        
        public Validation<Error, Option<IEnumerable<string>>> CanProcess(string matcher, Config config)
        {
            (IEnumerable<Error> errors, IEnumerable<string> results) = PathResolver
                .ResolvePathOrGlob(fileSystem, matcher, config.Location.DirectoryName)
                .Partition();

            if (errors.Any())
            {
                return errors.ToSeq();
            }

            return results.Any() && results.All(p => Path.GetExtension(p) == ProjectFileExtension)
                ? Some(results)
                : None;
        }

        public async IAsyncEnumerable<TestCase> Load(IEnumerable<string> resolvedSpecifications, ImporterContext context)
        {
            string filter = context.Include.Filter;
            if (filter is not null)
            {
                logger.LogDebug($"Filtering Audacity 3 tracks by name using filter '{filter}'.");
            }
            
            double temporalTolerance = context.Include.TemporalTolerance ?? defaultTolerance;
            double spectralTolerance = context.Include.SpectralTolerance ?? defaultTolerance;
            Override overrideBounds = context.Include.Override;

            foreach (string path in resolvedSpecifications)
            {
                logger.LogTrace("Loading Audacity 3 data file: {file}", path);
                
                Project dataFile = serializer.Deserialize((FileInfoBase) new FileInfo(path));

                int filteredCount = 0;
                int availableCount = 0;
                Arr<IExpectation> expectations = dataFile.Tracks
                    .SelectMany((track, trackIndex) =>
                        track.Labels
                            .Where(label =>
                            {
                                availableCount += 0;
                                bool included = filter == null || track.Name.Contains(filter);
                                if (!included)
                                {
                                    filteredCount += 1;
                                }

                                return included;
                            })
                            .Select((label, labelIndex) =>
                            {
                                string name =
                                    $"Audacity Label {label.Title} in track {track.Name} ({trackIndex}:{labelIndex})";

                                return label.IsSelPoint
                                    ? (IExpectation)new TemporalExpectation(label)
                                    {
                                        Time = new TimeRange(
                                            label.TimeStart.WithTolerance(temporalTolerance),
                                            label.TimeEnd.WithTolerance(temporalTolerance)),
                                        AnyLabel = new[] {label.Title},
                                        Name = name
                                    }
                                    : (IExpectation)new BoundedExpectation(label)
                                    {
                                        Bounds = new Bounds(
                                            overrideBounds?.Start ?? label.TimeStart.WithTolerance(temporalTolerance),
                                            overrideBounds?.End ?? label.TimeEnd.WithTolerance(temporalTolerance),
                                            overrideBounds?.Low ?? label.SelLow.WithTolerance(spectralTolerance),
                                            overrideBounds?.High ?? label.SelHigh.WithTolerance(spectralTolerance)),
                                        AnyLabel = new[] {label.Title},
                                        Name = name
                                    };
                            }))
                    .ToArr();

                if (context.Include.Exhaustive is bool exhaustive)
                {
                    expectations.Add(new NoExtraResultsExpectation {Match = exhaustive});
                }

                if (filteredCount > 0)
                {
                    logger.LogDebug(
                        $"Found {availableCount} and filtered out {filteredCount} keeping {expectations.Count} expectations.");
                }

                logger.LogTrace("Data file converted to expectations: {@expectations}", expectations);

                yield return new TestCase
                {
                    SourceInfo = dataFile.SourceInfo, 
                    Expect = expectations.ToArray(), 
                    
                    // TODO: audacity projects can store audio files with the project file,
                    //       but the audio files are split into multiple smaller files.
                    // File = Path.GetRelativePath(Path.GetDirectoryName(path), testFile)
                };
            }
        }
        
        private IExpectation MakeNoEventsExpectation(string path)
        {
            logger.LogWarning("No annotations found in {path}, producing a no_events expectation", path);
            return new NoEvents();
        }
    }
}