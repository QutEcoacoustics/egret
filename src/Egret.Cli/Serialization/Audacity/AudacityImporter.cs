namespace Egret.Cli.Serialization.Audacity
{
    using LanguageExt;
    using LanguageExt.Common;
    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Models;
    using Models.Audacity;
    using Models.Expectations;
    using Processing;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using static LanguageExt.Prelude;

    public class AudacityImporter : ITestCaseImporter
    {
        private const string ProjectFileExtension = ".aup";
        private const string Project3FileExtension = ".aup3";

        private readonly double defaultTolerance;
        private readonly IFileSystem fileSystem;
        private readonly ILogger<AudacityImporter> logger;
        private readonly AudacitySerializer serializer;
        private readonly Audacity3Serializer serializer3;

        public AudacityImporter(
            ILogger<AudacityImporter> logger,
            IFileSystem fileSystem,
            AudacitySerializer serializer,
            Audacity3Serializer serializer3,
            IOptions<AppSettings> settings)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.serializer = serializer;
            this.serializer3 = serializer3;
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

            return results.Any() && results.All(p =>
                Path.GetExtension(p) == ProjectFileExtension || Path.GetExtension(p) == Project3FileExtension)
                ? Some(results)
                : None;
        }

        public async IAsyncEnumerable<TestCase> Load(
            IEnumerable<string> resolvedSpecifications,
            ImporterContext context)
        {
            string filter = context.Include.Filter;
            if (filter is not null)
            {
                logger.LogDebug($"Filtering Audacity tracks by name using filter '{filter}'.");
            }

            double temporalTolerance = context.Include.TemporalTolerance ?? defaultTolerance;
            double spectralTolerance = context.Include.SpectralTolerance ?? defaultTolerance;
            Override overrideBounds = context.Include.Override;

            foreach (string path in resolvedSpecifications)
            {
                var ext = Path.GetExtension(path);
                Project dataFile;
                switch (ext)
                {
                    case ProjectFileExtension:
                    {
                        logger.LogTrace("Loading Audacity data file: {file}", path);
                        await using Stream stream = fileSystem.File.OpenRead(path);
                        dataFile = serializer.Deserialize(stream, path);
                        break;
                    }
                    case Project3FileExtension:
                        logger.LogTrace("Loading Audacity 3 data file: {file}", path);
                        dataFile = serializer3.Deserialize((FileInfoBase)new FileInfo(path));
                        break;
                    default:
                        throw new ArgumentException(
                            $"Could not load Audacity data file. Is this an Audacity project file at path '{path}'?");
                }

                int filteredCount = 0;
                int availableCount = 0;
                Arr<IExpectation> expectations = dataFile.Tracks
                    .SelectMany((track, trackIndex) =>
                        track.Labels
                            .Where(label =>
                            {
                                availableCount += 1;

                                bool included = filter == null || track.Name.Contains(filter);
                                if (included)
                                {
                                    logger.LogTrace(
                                        "Included Audacity project label '{label}' " +
                                        "because the track '{track}' matched filter '{filter}'.",
                                        label, track.Name, filter);
                                }
                                else
                                {
                                    logger.LogTrace(
                                        "Discarded Audacity project label '{label}' " +
                                        "because the track '{track}' did not match filter '{filter}'.",
                                        label, track.Name, filter);
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

                if (expectations.IsEmpty)
                {
                    logger.LogWarning("No annotations found in {path}, producing a no events expectation", path);
                    expectations = new Arr<IExpectation> {new NoEvents()};
                }

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

                // find the associated audio file
                var pathDir = Path.GetDirectoryName(path);
                var audioFileMatcher = new Matcher();
                audioFileMatcher.AddInclude(Path.GetFileNameWithoutExtension(path) + ".*");

                var knownAudioExts = new[] {".wav", ".mp3", ".ogg", ".flac", ".wv", ".webm", ".aiff", ".wma", ".m4a"};
                var matches = audioFileMatcher
                    .GetResultsInFullPath(fileSystem, pathDir)
                    .Where(p => knownAudioExts.Contains(Path.GetExtension(p)))
                    .ToArr();

                if (matches.Count != 1)
                {
                    var foundFiles = matches
                        .Select(Path.GetFileName)
                        .OrderBy(i => i)
                        .ToArr();
 
                    var foundFileString = string.Join(", ", foundFiles.IsEmpty ? new string[] {"No audio files found"} : foundFiles);
                    var extString = string.Join(", ", knownAudioExts);
                    throw new FileNotFoundException(
                        $"Could not find the one audio file (with extensions {extString}) for Audacity project file '{path}': {foundFileString}.");
                }

                yield return new TestCase
                {
                    SourceInfo = dataFile.SourceInfo,
                    Expect = expectations.ToArray(),
                    File = Path.GetRelativePath(pathDir, matches.First()),
                };
            }
        }
    }
}