using Egret.Cli.Models;
using Egret.Cli.Serialization;
using Egret.Cli.Serialization.Egret;
using Egret.Tests.Support;
using FluentAssertions;
using FluentAssertions.Common;
using LanguageExt;
using LanguageExt.Common;
using MoreLinq.Extensions;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using YamlDotNet.Serialization.NamingConventions;

namespace Egret.Tests.Serialization.Egret
{
    public class EgretImporterTests : TestBase
    {
        public static readonly TestFile ExampleConfig = (Helpers.DefaultTestConfigPath, @"
test_suites:
  ABC:
    tool_configs:
    label_aliases: []
    include_tests:
      # include all the false positives, except for powerful owl, from the common_tests section
      - from: /other_config.egret.yaml
        filter:
");
        public static readonly TestFile ExampleConfigFilter = ("/config2.egret.yaml", @"
test_suites:
  ABC:
    tool_configs:
    label_aliases: []
    include_tests:
      # include all the false positives, except for powerful owl, from the common_tests section
      - from: ./other_config.egret.yaml
        filter: '!*POWL'
");
        public static readonly TestFile OtherConfig = ("/other_config.egret.yaml", @"
test_suites:
  1 POWL:
    tool_configs:
    label_aliases: []
    tests:
        - name: D
          file: bird.wav
          expect:
            - label: NinoxBoobook
              bounds: [≈0.069, 420, ≈0.84, '<500']
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

        public EgretImporterTests(ITestOutputHelper output) : base(output)
        {
            deserializer = BuildConfigDeserializer();

            TestFiles.AddFile(ExampleConfig);
            TestFiles.AddFile(ExampleConfigFilter);
            TestFiles.AddFile(OtherConfig);
            TestFiles.AddFile("bird.wav", "");
        }

        public static TheoryData<TestFile> Examples => new() { ExampleConfig, ExampleConfigFilter };

        private async Task<Config> RunExample(TestFile config)
        {
            using var reader = new StringReader(config.Contents);
            var result = await deserializer.Deserialize(reader, config.Path);

            result.Errors.Should().BeEmpty();
            return result.Config;
        }


        [Theory]
        [MemberData(nameof(Examples))]
        public async Task HostCommonTestsAreUnaffected(TestFile file)
        {
            var config = await RunExample(file);
            config.CommonTests.Should().BeEmpty();
        }


        [Theory]
        [MemberData(nameof(Examples))]
        public async Task ThereIsOneHostTestSuite(TestFile file)
        {
            var config = await RunExample(file);

            config.TestSuites.Should().HaveCount(1).And.ContainKey("ABC");
            config.TestSuites["ABC"].Name.Should().Be("ABC");
        }

        [Theory]
        [MemberData(nameof(Examples))]
        public async Task TheHostTestSuiteContextIsUnchanged(TestFile file)
        {
            var config = await RunExample(file);
            config.TestSuites["ABC"].SourceInfo.ToString().Should().Be(new SourceInfo(file.Path, 4, 5, 10, 1).ToString());
            config.TestSuites["ABC"].SourceInfo.Should().Be(new SourceInfo(file.Path, 4, 5, 10, 1));
        }

        [Fact]
        public async Task GetAllTestsWillReturnIncludeTests()
        {
            var config = await RunExample(ExampleConfig);

            var suite = config.TestSuites["ABC"];
            Assert.Empty(suite.Tests);

            var actualTests = suite.GetAllTests().Select(x => x.Name);

            actualTests
                .Should().HaveCount(4)
                .And.BeEquivalentTo("A", "B", "C", "D");
        }

        [Fact]
        public async Task GetAllTestsWillReturnIncludeTests_2()
        {
            var config = await RunExample(ExampleConfigFilter);

            var suite = config.TestSuites["ABC"];
            Assert.Empty(suite.Tests);

            var actualTests = suite.GetAllTests().Select(x => x.Name);

            actualTests
                .Should().HaveCount(2)
                .And.BeEquivalentTo("B", "C");
        }

        [Theory]
        [MemberData(nameof(Examples))]
        public async Task EachIncludedTestHasTheCorrectSource(TestFile file)
        {
            var config = await RunExample(file);
            config
                .TestSuites["ABC"]
                .IncludeTests
                .SelectMany(ti => ti.Tests.Select(t => t.SourceInfo.Source))
                .AsEnumerable()
                .Should()
                .AllBe(TestFiles.Path.GetFullPath(OtherConfig.Path));
        }


        [Fact]
        public async Task IncludesTestsAreCorrect()
        {
            var config = await RunExample(ExampleConfig);

            var suite = config.TestSuites["ABC"];
            Assert.Empty(suite.Tests);

            var actualTests = suite.IncludeTests.SelectMany(x => x.Tests.Select(t => t.Name)).AsEnumerable();

            actualTests
                .Should().HaveCount(4)
                .And.BeEquivalentTo("A", "B", "C", "D");
        }

        [Fact]
        public async Task IncludesTestsAreCorrect_2()
        {
            var config = await RunExample(ExampleConfigFilter);

            var suite = config.TestSuites["ABC"];
            Assert.Empty(suite.Tests);

            var actualTests = suite.IncludeTests.SelectMany(x => x.Tests.Select(t => t.Name)).AsEnumerable();

            actualTests
                .Should().HaveCount(2)
                .And.BeEquivalentTo("B", "C");
        }

    }
}