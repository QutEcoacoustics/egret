using Egret.Cli.Processing;
using Egret.Tests.Support;
using FluentAssertions;
using LanguageExt;
using static LanguageExt.Prelude;
using System;
using Xunit;
using Xunit.Abstractions;
using System.Text.RegularExpressions;
using MoreLinq;

namespace Egret.Tests.Processing
{
    public class ToolRunnerTests : TestBase
    {
        private readonly ToolRunner runner;

        public ToolRunnerTests(ITestOutputHelper output) : base(output)
        {
            runner = new ToolRunner(BuildLogger<ToolRunner>(), TempFactory, AppSettings);
        }

        [Fact]
        public void CommandTemplaterFailsIfArgumentsAreNotTemplated()
        {
            var placeholders = new CommandPlaceholders("/source.wav", "/out_dir/", "/TempDir/", "/");
            var template = "audio2csv {source} {output} --no-debug";

            var result = runner.PrepareArguments(template, new(), placeholders);

            result.Should().Be("audio2csv /source.wav /out_dir/ --no-debug");

            var faultyTemplate = "audio2csv {source} {config} {output} --no-debug";

            Action act = () => runner.PrepareArguments(faultyTemplate, new(), placeholders);

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("Could not finish templating command. Missing a value for parameter `{config}`");
        }

        [Theory]
        [InlineData("v\\d+")]
        [InlineData("(v)(\\d+)")]
        public void VersionRegexRequiresACaptureGroup(string pattern)
        {
            Action act = () => ToolRunner.GetVersion(new Regex(pattern), null);

            act.Should().Throw<ArgumentException>("Version regex requires exactly one capture group");
        }

        [Fact]
        public void VersionRegexWorksWithOutput()
        {
            var processResult = new ProcessResult(true, 0, output: "\nabcdef_v123defg\n", "", null);
            var version = ToolRunner.GetVersion(new Regex("(v\\d+)"), processResult);

            Assert.Equal(Some("v123"), version);
        }

        [Fact]
        public void VersionRegexWorksWithError()
        {
            var processResult = new ProcessResult(true, 0, "", error: "\nabcdef_v123defg\n", null);
            var version = ToolRunner.GetVersion(new Regex("(v\\d+)"), processResult);

            Assert.Equal(Some("v123"), version);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("(notamatch)")]
        public void VersionRegexDoesNotErrorOnNullOrNonMatch(string pattern)
        {
            var regex = pattern is null ? null : new Regex(pattern);
            var processResult = new ProcessResult(true, 0, "", error: "\nabcdef_v123defg\n", null);
            var version = ToolRunner.GetVersion(regex, processResult);

            Assert.Equal(None, version);
        }
    }
}