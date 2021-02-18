using Egret.Cli.Processing;
using Egret.Tests.Support;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Xunit;
using Xunit.Abstractions;
using static LanguageExt.Prelude;

namespace Egret.Tests.Processing
{
    public class PathResolverTests : TestBase
    {
        private readonly string workingDirectory;

        public PathResolverTests(ITestOutputHelper output) : base(output)
        {
            TestFiles.AddFile("/abc/123.wav", "");
            TestFiles.AddFile("/def/456.wav", "");

            workingDirectory = "/abc";
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void NullOrEmptySimplyReturnsEmpty(string spec)
        {
            var result = PathResolver.ResolvePathOrGlob(TestFiles, spec, workingDirectory);

            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData("/def/456.wav")]
        [InlineData("../def/456.wav")]
        [InlineData("../def/*.wav")]
        public void TestResolveWorks(string spec)
        {
            var result = PathResolver.ResolvePathOrGlob(TestFiles, spec, workingDirectory);

            result.Should().Equal(this.ResolvePath("/def/456.wav"));
        }

        [Theory]
        [InlineData("/abc/123.wav")]
        [InlineData("../abc/123.wav")]
        [InlineData("../abc/*.wav")]
        public void TestResolveWorks2(string spec)
        {
            var result = PathResolver.ResolvePathOrGlob(TestFiles, spec, workingDirectory);

            result.Should().Equal(this.ResolvePath("/abc/123.wav"));
        }

        [Fact]
        public void ResolveFailsIfAbsolutePathDoesNotExist()
        {
            PathResolver
                .ResolvePathOrGlob(TestFiles, "/i_don't_exist", workingDirectory)
                .Should()
                .Equal(
                    FinFail<string>(
                        Error.New("Path`/i_don't_exist` does not exist. Is the path correct?")));
        }

        [Fact]
        public void ResolveFailsIfGlobExpandsToNothing()
        {
            PathResolver
                .ResolvePathOrGlob(TestFiles, "*donkey*", workingDirectory)
                .Should()
                .Equal(
                    FinFail<string>(
                        Error.New("Expanding pattern `*donkey*` produced no files. Is that pattern correct?")));
        }

        [Fact]
        public void ResolveFailsIfMalformedGlob()
        {
            PathResolver
                .ResolvePathOrGlob(TestFiles, "*||*", workingDirectory)
                .Should()
                .Equal(
                    FinFail<string>(
                        Error.New("There is an empty segment in multiglob `*||*` pattern is too short, there is nothing to match against")));
        }
    }
}