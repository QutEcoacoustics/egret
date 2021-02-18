using Egret.Cli.Models;
using Egret.Cli.Serialization;
using Egret.Tests.Support;
using FluentAssertions;
using FluentAssertions.Common;
using LanguageExt;
using LanguageExt.Common;
using MoreLinq.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using YamlDotNet.Serialization.NamingConventions;

namespace Egret.Tests.Serialization.Egret
{
    public class SharedImporterTests : TestBase
    {
        public static readonly TestFile ExampleConfig = ("./config.egret.yaml", @"
test_suites:
  1 POWL:
    tool_configs:
    label_aliases: []
    include_tests:
      # include all the false positives, except for powerful owl, from the common_tests section
      - from: common_tests
        filter: negatives_*|!negatives_POWL
common_tests:
    negatives_POWL:
        - name: A
          expect: [ { label: 'a', match: false } ]
    negatives_boobook:
        - name: B
          expect: [ { label: 'b', match: false  } ]
    negatives_koala:
        - name: C
          expect: [ { segment_with: 'no_events' } ]
");
        private readonly ConfigDeserializer deserializer;

        private readonly TestCase A = new()
        {
            Name = "A",
            Expect = new IExpectation[]
            {
                new LabelOnlyExpectation() { Label = "a", Match = false },
                new LabelPresent() { Label="a", Match= false },
            },
            SourceInfo = new SourceInfo(Helpers.DefaultTestConfigPath, 11, 11, 14, 5)
        };
        private readonly TestCase B = new()
        {
            Name = "B",
            Expect = new IExpectation[]
            {
                new LabelOnlyExpectation() { Label = "b", Match = false },
                new LabelPresent() { Label = "b", Match = false },
            },
            SourceInfo = new SourceInfo(Helpers.DefaultTestConfigPath, 14, 11, 16, 5)
        };
        private readonly TestCase C = new()
        {
            Name = "C",
            Expect = new IExpectation[]
            {
                new NoEvents()
            },
            SourceInfo = new SourceInfo(Helpers.DefaultTestConfigPath, 17, 11, 19, 1)
        };

        public SharedImporterTests(ITestOutputHelper output) : base(output)
        {

            deserializer = BuildConfigDeserializer();

            TestFiles.AddFile(ExampleConfig);
        }

        private async Task<Config> RunExample(TestFile config)
        {
            using var reader = new StringReader(config.Contents);
            var result = await deserializer.Deserialize(reader, Helpers.DefaultTestConfigPath);

            result.Errors.Should().BeEmpty();
            return result.Config;
        }

        [Fact]
        public async Task CommonTestsAreUnaffected()
        {
            var config = await RunExample(ExampleConfig);

            config.CommonTests.Should().ContainKeys("negatives_POWL", "negatives_boobook", "negatives_koala");
            var cases = config.CommonTests.Select(kvp => kvp.Value.Single().Name).Should().BeEquivalentTo(new[] { A.Name, B.Name, C.Name });
        }


        [Fact]
        public async Task ThereIsOneTestSuite()
        {
            var config = await RunExample(ExampleConfig);

            config.TestSuites.Should().HaveCount(1);
            config.TestSuites.First().Key.Should().Be("1 POWL");
        }

        [Fact]
        public async Task TheTestSuiteHasContextIsUnchanged()
        {
            var config = await RunExample(ExampleConfig);

            var suite = config.TestSuites.First().Value;
            suite.Name.Should().Be("1 POWL");
            suite.SourceInfo.Source.Should().Be(Helpers.DefaultTestConfigPath);
        }

        [Fact]
        public async Task GetAllTestsWillReturnIncludeTests()
        {
            var config = await RunExample(ExampleConfig);

            var suite = config.TestSuites.First().Value;
            Assert.Empty(suite.Tests);
            var actualTests = suite.GetAllTests().Select(x => x.Name);

            actualTests
                .Should().HaveCount(2)
                .And.BeEquivalentTo(B.Name, C.Name);
        }

        [Fact]
        public async Task TheIncludeEntryIsWellFormedAndLoadsTestCases()
        {
            var config = await RunExample(ExampleConfig);

            var include = config.TestSuites.First().Value.IncludeTests.First();

            // does not load A because filtered out in the from specification
            include.From.Should().Be("common_tests");
            include.Filter.Should().Be("negatives_*|!negatives_POWL");
            var names = include.Tests.Select(t => t.Name).AsEnumerable();

            names.Should().BeEquivalentTo(B.Name, C.Name);
        }
    }
}

