using Egret.Cli.Models;
using Egret.Cli.Processing;
using Egret.Cli.Serialization.Avianz;
using Egret.Cli.Serialization.Yaml;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.Disposables.Internals;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using static LanguageExt.Prelude;

namespace Egret.Cli.Serialization
{
    public class ConfigDeserializer
    {
        private ILogger<ConfigDeserializer> Logger { get; }

        private readonly INamingConvention NamingConvention;
        private readonly TestCaseImporter importer;
        private SourceInfoNodeDeserializer sourceInfoNodeDeserializer;

        public ConfigDeserializer(ILogger<ConfigDeserializer> logger, IOptions<AppSettings> settings, TestCaseImporter importer)
        {
            Logger = logger;
            NamingConvention = UnderscoredNamingConvention.Instance;
            this.importer = importer;

            // these resolvers allow us to deserialize to an abstract class or interface
            var aggregateExpectationResolver = new AggregateExpectationTypeResolver(NamingConvention);
            var expectationResolver = new ExpectationTypeResolver(NamingConvention);


            YamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(NamingConvention)
                .WithTypeConverter(new IntervalTypeConverter(settings.Value.DefaultThreshold))
                .WithNodeDeserializer(
                    inner =>
                    {
                        var sourceInfo = new SourceInfoNodeDeserializer(
                            new AbstractNodeNodeTypeResolver(inner, aggregateExpectationResolver, expectationResolver));
                        sourceInfoNodeDeserializer = sourceInfo;
                        return sourceInfo;
                    },
                     s => s.InsteadOf<ObjectNodeDeserializer>())
                .WithNodeDeserializer(
                    inner => new DictionaryKeyPreserverNodeDeserializer(inner),
                     s => s.InsteadOf<DictionaryNodeDeserializer>())
                // more: https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer
                .Build();
        }

        public IDeserializer YamlDeserializer { get; private set; }
        public IEnumerable<ITestCaseImporter> Importers { get; private set; }

        public async Task<(Config, Seq<Error>)> Deserialize(FileInfo configFile)
        {
            Logger.LogDebug("Loading config file: {file}", configFile);
            sourceInfoNodeDeserializer.CurrentSource = configFile.FullName;
            using var reader = configFile.OpenText();

            var config = YamlDeserializer.Deserialize<Config>(reader);
            sourceInfoNodeDeserializer.CurrentSource = null;

            Logger.LogDebug("Normalizing config file: {file}", configFile);
            var errors = await Normalize(config, configFile);

            // TODO: call validation?

            Logger.LogDebug("Finished loading config file: {file}", configFile);
            return (config, errors);
        }

        private async ValueTask<Seq<Error>> Normalize(Config original, FileInfo filePath)
        {
            original.Location = filePath;
            Seq<Error> errors = Empty;

            Logger.LogDebug("Loading includes and tests for test suites");
            foreach (var (name, suite) in original.TestSuites)
            {
                // resolve includes
                errors += await importer.LoadImportedTestCases(suite, original);

                var newTests = NormalizeTests(suite.Tests);
                var newIncludes = suite.IncludeTests.Select(include => include with { Tests = NormalizeTests(include.Tests) });
                suite.Tests = newTests;
                suite.IncludeTests = newIncludes.ToArray();
            }

            return errors;
        }

        private TestCase[] NormalizeTests(TestCase[] tests)
        {
            return tests.SelectMany(ExpandFileGlobs).Select(GenerateAutoLabelPresenceSegmentTest).ToArray();
        }

        //TODO
        public static IEnumerable<TestCase> ExpandFileGlobs(TestCase testCase)
        {
            if (testCase?.File is { Length: < 1 } || !MultiGlob.TestIfMultiGlob(testCase.File))
            {
                yield return testCase;
                yield break;
            }

            var glob = MultiGlob.Parse(testCase.File);
            var directory = Path.GetDirectoryName(testCase.SourceInfo.Source);
            var count = 0;
            foreach (var result in glob.GetResultsInFullPath(directory))
            {
                yield return testCase with
                {
                    Name = (testCase.Name ?? "") + "#" + count,
                    File = result,
                    // TODO: needed?
                    SourceInfo = testCase.SourceInfo,
                };

                count++;
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
        /// /// necessary segment-level annotation.
        /// </summary>
        /// <param name="cases"></param>
        /// <returns></returns>
        TestCase GenerateAutoLabelPresenceSegmentTest(TestCase test)
        {

            // first collect unique tags
            var eventExcpectations = test.Expect.OfType<Expectation>();
            var expectedLabels = eventExcpectations
                .SelectMany(e => e.AnyLabel.Append(e.Label))
                .WhereNotNull()
                .Distinct();

            // now see if we already have any label present expectations
            var segmentExcpectations = test.Expect.OfType<LabelPresent>().ToDictionary(x => x.Label);
            var newExpectations = Lst<IExpectation>.Empty;
            foreach (var label in expectedLabels)
            {
                if (segmentExcpectations.TryGetValue(label, out var labelPresent))
                {
                    // we don't need another one
                    continue;
                }

                // nothing present, let's add one
                newExpectations += new LabelPresent() { Label = label };
            }

            // return the test with augmented expectations
            return test with
            {
                Expect = test.Expect.Append(newExpectations).ToArray()
            };
        }
    }

}
