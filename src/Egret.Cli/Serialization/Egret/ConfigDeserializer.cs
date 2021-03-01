using Egret.Cli.Models;
using Egret.Cli.Models.Expectations;
using Egret.Cli.Processing;
using Egret.Cli.Serialization.Avianz;
using Egret.Cli.Serialization.Yaml;
using LanguageExt;
using LanguageExt.ClassInstances;
using LanguageExt.Common;
using LanguageExt.TypeClasses;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.Disposables.Internals;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization.Utilities;
using static LanguageExt.Prelude;

namespace Egret.Cli.Serialization
{
    public class ConfigDeserializer
    {
        private ILogger<ConfigDeserializer> Logger { get; }

        private readonly IFileSystem fileSystem;

        private readonly INamingConvention NamingConvention;

        private readonly TestCaseImporter importer;

        private readonly IOptions<AppSettings> settings;

        public ConfigDeserializer(
            ILogger<ConfigDeserializer> logger,
            IOptions<AppSettings> settings,
            TestCaseImporter importer,
            INamingConvention namingConvention,
            IFileSystem fileSystem)
        {
            Logger = logger;
            NamingConvention = namingConvention;
            this.fileSystem = fileSystem;
            this.importer = importer;
            this.settings = settings;

        }
        public IEnumerable<ITestCaseImporter> Importers { get; private set; }

        public Task<(Config, Seq<Error>)> Deserialize(IFileInfo configFile)
        {
            using var reader = configFile.OpenText();
            return Deserialize(reader, configFile.FullName);
        }

        public async Task<(Config Config, Seq<Error> Errors)> Deserialize(System.IO.TextReader reader, string configFilePath)
        {
            Logger.LogDebug("Loading config file: {file}", configFilePath);
            var deserializer = BuildDeserializer(configFilePath);

            var config = deserializer.Deserialize<Config>(reader);

            Logger.LogDebug("Normalizing config file: {file}", configFilePath);
            var errors = await Normalize(config, fileSystem.FileInfo.FromFileName(configFilePath));

            // TODO: call validation?

            Logger.LogDebug("Finished loading config file: {file}", configFilePath);
            return (config, errors);
        }

        /// <summary>
        /// We build a new instance of a deserializer for every operation so that
        /// we can provide source context (a file path) to every read operation.
        /// </summary>
        internal IDeserializer BuildDeserializer(string context)
        {
            // these resolvers allow us to deserialize to an abstract class or interface
            var aggregateExpectationResolver = new AggregateExpectationTypeResolver(NamingConvention);
            var expectationResolver = new ExpectationTypeResolver(NamingConvention);

            return new DeserializerBuilder()
                .WithNamingConvention(NamingConvention)
                .WithTypeConverter(new IntervalTypeConverter(settings.Value.DefaultThreshold))
                .WithNodeDeserializer(
                    inner =>
                    {
                        return new SourceInfoNodeDeserializer(
                            new AbstractNodeNodeTypeResolver(inner, aggregateExpectationResolver, expectationResolver),
                            context);
                    },
                    s => s.InsteadOf<ObjectNodeDeserializer>())
                .WithNodeDeserializer(
                    inner => new DictionaryKeyPreserverNodeDeserializer(inner),
                     s => s.InsteadOf<DictionaryNodeDeserializer>())
                .WithNodeDeserializer(new ArrNodeDeserializer())
                .WithNodeDeserializer(new AliasedStringNodeDeserializer())

                // more: https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer
                .Build();
        }

        private async ValueTask<Seq<Error>> Normalize(Config original, IFileInfo filePath)
        {
            original.Location = filePath;
            Seq<Error> errors = Empty;

            Logger.LogDebug("Loading includes and tests for test suites");
            foreach (var (name, suite) in original.TestSuites)
            {
                // resolve includes
                errors += await importer.LoadImportedTestCases(suite, original, this);

                var (newErrors, newTests) = NormalizeTests(suite.Tests);
                errors += newErrors.ToSeq();
                var newIncludes = suite.IncludeTests.Select(include =>
                {
                    var (includeErrors, includeTests) = NormalizeTests(include.Tests);
                    errors += newErrors.ToSeq();
                    return include with { Tests = includeTests.ToArr() };
                });
                suite.Tests = newTests.ToArr();
                suite.IncludeTests = newIncludes;
            }

            return errors;
        }

        public (IEnumerable<Error> Errors, IEnumerable<TestCase> Tests) NormalizeTests(Arr<TestCase> tests)
        {
            var normalized = tests
                .Collect(t => ExpandFileGlobs(fileSystem, t))
                .Select(GenerateAutoLabelPresenceSegmentTest);

            return normalized.Partition();
        }

        public static IEnumerable<Fin<TestCase>> ExpandFileGlobs(IFileSystem fileSystem, TestCase testCase)
        {
            if (testCase?.File is null or "")
            {
                yield return testCase;
                yield break;
            }

            var directory = fileSystem.Path.GetDirectoryName(testCase.SourceInfo.Source);
            var results = PathResolver.ResolvePathOrGlob(fileSystem, testCase.File, directory).ToArr();

            int count = 0;
            bool moreThanOne = results.Count > 1;
            foreach (var result in results)
            {
                count++;
                yield return result.Map(Make);
            }

            TestCase Make(string path)
            {
                var newName = testCase.Name switch
                {
                    string s when !moreThanOne => s,
                    null or "" => "#" + count,
                    string s when Regex.Match(s, "#\\d+$").Success => s,
                    string s => s + "#" + count
                };

                return testCase with
                {
                    Name = newName,
                    File = path,
                    SourceInfo = testCase.SourceInfo,
                };
            }
        }

        /// <summary>
        /// Automatically generates a segment-level label presence expectation for each tag type tested by an expectation.
        /// i.e. If we have an expectation that tests for a  Koala between [1.5, 500, 3.5, 1200], that event-level expectation exists.
        /// So we also generate a LabelPresence segment-level expectation so we can test if any Koala labelled event exists within the segment.
        /// This automatic test generation allows us to generate both event and segment level tests easily.
        /// Note: the original method we used to infer segment-level tests was based on inference of expectations after evaluating the results.
        /// The net result was confusing because a single expectation could be both a false positive and a true positive at the same time!
        /// Thus I thought it better to simply treat each single expectation as a single one - as they were intended - and generate the
        /// necessary segment-level annotation.
        /// </summary>
        /// <param name="cases"></param>
        /// <returns></returns>
        public static TestCase GenerateAutoLabelPresenceSegmentTest(TestCase test)
        {

            // first collect unique tags
            var eventExcpectations = test.Expect.OfType<EventExpectation>();
            var expectedLabels = eventExcpectations.SelectMany(Extract).Distinct();

            // now see if we already have any label present expectations
            var segmentExcpectationsMatch = test.Expect.OfType<LabelPresent>().Where(x => x.Match).ToDictionary(x => x.Label);
            var segmentExcpectationsNoMatch = test.Expect.OfType<LabelPresent>().Where(x => x.Match).ToDictionary(x => x.Label);
            var newExpectations = Lst<IExpectation>.Empty;
            foreach (var (label, match) in expectedLabels)
            {
                var relevantList = match ? segmentExcpectationsMatch : segmentExcpectationsNoMatch;
                if (relevantList.TryGetValue(label, out var labelPresent))
                {
                    // we don't need another one
                    continue;
                }

                // nothing present, let's add one
                newExpectations += new LabelPresent() { Label = label, Match = match };
            }

            // return the test with augmented expectations
            return test with
            {
                Expect = test.Expect.AddRange(newExpectations)
            };

            IEnumerable<(string Label, bool match)> Extract(EventExpectation e)
                => e.AnyLabel.Append(e.Label).WhereNotNull().Select(l => (l, e.Match));
        }

        /// <summary>
        /// Automatically generate an exhaustiveness check expectation.
        /// This will ensure any extra results are reported as errors
        /// </summary>
        public static TestCase GenerateNoExtraResultsExpectation(TestCase test)
        {
            // assume expectations are exhaustive, add an automatic test for 
            // no extra results.
            // TODO: make this an configurable option?
            if (test.Expect.OfType<NoExtraResultsExpectation>().Any())
            {
                // expectation already defined in config, nothing else to do
                return test;
            }

            return test with
            {
                Expect = test.Expect.Add(new NoExtraResultsExpectation())
            };
        }
    }

}
