using Egret.Cli.Models;
using Egret.Cli.Processing;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using static LanguageExt.Prelude;

namespace Egret.Cli.Serialization.Egret
{
    /// <summary>
    /// Imports test suites from another egret configuration file
    /// </summary>
    public class EgretImporter : ITestCaseImporter
    {
        private static int recursionCounter = 0;

        public static readonly IReadOnlyList<string> EgretConfigExtension = new[] {
            ".egret.yml",
            ".egret.yaml"
        };
        private readonly ILogger<EgretImporter> logger;
        private readonly IFileSystem fileSystem;

        public EgretImporter(ILogger<EgretImporter> logger, IFileSystem fileSystem)
        {
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public static bool IsEgretConfigFile(string path)
        {
            Debug.Assert(EgretConfigExtension.Count == 2, "hardcased for 2 items");

            return path.EndsWith(EgretConfigExtension[0]) || path.EndsWith(EgretConfigExtension[1]);
        }

        public Validation<Error, Option<IEnumerable<string>>> CanProcess(string matcher, Config config)
        {
            var (errors, results) = PathResolver
                .ResolvePathOrGlob(fileSystem, matcher, config.Location.DirectoryName)
                .Partition();

            if (errors.Any())
            {
                return errors.ToSeq();
            }
            return results.Any() && results.All(IsEgretConfigFile) ? Some(results) : None;
        }

        public async IAsyncEnumerable<TestCase> Load(IEnumerable<string> resolvedSpecifications, ImporterContext context)
        {
            var scope = logger.BeginScope("Recursion: {recursionCounter}", Interlocked.Increment(ref recursionCounter));
            try
            {
                if (context.Include.SpectralTolerance is not null || context.Include.TemporalTolerance is not null)
                {
                    logger.LogWarning(
                        $"{nameof(TestCaseInclude.SpectralTolerance)} and {nameof(TestCaseInclude.TemporalTolerance)} are ignored when importing tests from another egret config file. You can put tolerances on the tests directly.");
                }

                foreach (var path in resolvedSpecifications)
                {
                    // warning: recursive
                    await foreach (var test in LoadExternalConfig(path, context))
                    {
                        yield return test;
                    }
                }
            }
            finally
            {
                scope.Dispose();
                Interlocked.Decrement(ref recursionCounter);
            }
        }

        private async IAsyncEnumerable<TestCase> LoadExternalConfig(string path, ImporterContext context)
        {
            var filter = context.Include.FilterMatcher;

            using var _ = logger.BeginScope("Loading included config {path}", path);
            using var stream = fileSystem.File.OpenText(path);

            // warning: recursive
            var (loadedConfig, errors) = await context.Deserializer.Deserialize(stream, path);

            if (errors.Any())
            {
                logger.LogError("Error while reading included config {path}: {@errors}", path, errors);
                throw new Exception($"Error while reading included config {path}:\n" + errors.Select(e => e.Message).Join("\n  - ", "\""));
            }

            // for each
            // suite
            //   check if suite name matches the filter
            foreach (var (key, suite) in loadedConfig.TestSuites)
            {
                if (filter.Match(key).HasMatches)
                {
                    logger.LogTrace("Test suite with name {name} matched filter {filter}", key, filter.OriginalPattern);
                    foreach (var test in suite.GetAllTests())
                    {
                        yield return test;
                    }
                }
                else
                {
                    logger.LogTrace("Test suite with name {name} did not match filter {filter}", key, filter.OriginalPattern);
                    continue;
                }
            }

            // common_tests
            //   check if each grouping's name matches the filter
            foreach (var (key, tests) in loadedConfig.CommonTests)
            {
                if (filter.Match(key).HasMatches)
                {
                    logger.LogTrace("Common test group with name {name} matched filter {filter}", key, filter.OriginalPattern);
                    foreach (var test in tests)
                    {
                        yield return test;
                    }
                }
                else
                {
                    logger.LogTrace("Common test group with name {name} did not match filter {filter}", key, filter.OriginalPattern);
                    continue;
                }
            }
        }
    }
}