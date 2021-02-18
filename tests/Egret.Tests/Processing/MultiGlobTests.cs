using Egret.Cli.Processing;
using Egret.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.FileSystemGlobbing;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Egret.Tests.Processing
{
    public class MultiGlobTests : TestBase
    {
        private static readonly string[] Source = new[] {
            "negatives_anthropogenic",
            "negatives_geophonic",
            "negatives_YBG",
            "negatives_barking_owl",
            "negatives_boobook",
            "negatives_brushtail possum",
            "negatives_dogs",
            "negatives_frogs_high",
            "negatives_frogs_low",
            "negatives_GHFF",
            "negatives_koala",
            "negatives_kookaburra",
            "negatives_owlet_nightjar",
            "negatives_POWL",
            "negatives_squirrel_glider",
            "negatives_sugar_glider",
        };

        public MultiGlobTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData("*", 16)]
        [InlineData("*ic|*_low|*_high", 4,
            "negatives_YBG",
            "negatives_barking_owl",
            "negatives_boobook",
            "negatives_brushtail possum",
            "negatives_dogs",
            "negatives_GHFF",
            "negatives_koala",
            "negatives_kookaburra",
            "negatives_owlet_nightjar",
            "negatives_POWL",
            "negatives_squirrel_glider",
            "negatives_sugar_glider")]
        [InlineData("negatives_*|!negatives_squirrel_glider", 15, "negatives_squirrel_glider")]
        [InlineData("!*", 0, "negatives_anthropogenic",
            "negatives_geophonic",
            "negatives_YBG",
            "negatives_barking_owl",
            "negatives_boobook",
            "negatives_brushtail possum",
            "negatives_dogs",
            "negatives_frogs_high",
            "negatives_frogs_low",
            "negatives_GHFF",
            "negatives_koala",
            "negatives_kookaburra",
            "negatives_owlet_nightjar",
            "negatives_POWL",
            "negatives_squirrel_glider",
            "negatives_sugar_glider")]
        public void MultiGlobWorkOnStrings(string pattern, int expectedCount, params string[] missingItems)
        {
            //Given
            var glob = MultiGlob.Parse(pattern);

            //When
            var actual = glob.Match(Source);

            //Then
            Assert.Equal(expectedCount > 0, actual.HasMatches);
            Assert.Equal(expectedCount, actual.Files.Count());
            var delta = Source.Except(missingItems);
            Assert.Equal(delta, actual.Files.Select(x => x.Path));
        }

        [Theory]
        [InlineData("!hello", "!anythingelse")]
        [InlineData("[!]hello", "!hello")]
        [InlineData("hel!lo", "hel!lo")]
        [InlineData("hel[!]lo", "hel!lo")]
        [InlineData("[!]hello|!donkey", "!hello")]
        public void MultiGlobCanStillMatchBangs(string pattern, string example)
        {
            var glob = MultiGlob.Parse(pattern); ;

            var actual = glob.Match(example);

            actual.HasMatches.Should().BeTrue();
        }

        [Fact]
        public void MultiGlobProvidesOriginalPattern()
        {
            var glob = MultiGlob.Parse("[!]hello|!donkey");

            glob.OriginalPattern.Should().Be("[!]hello|!donkey");
        }

        [Fact]
        public void MultiGlobHasAnIFileSystemExtension()
        {
            var glob = MultiGlob.Parse("**|!donkey");
            TestFiles.AddFile("hello.txt", null);
            TestFiles.AddFile("donkey", null);
            TestFiles.AddFile("abc/hello", null);
            TestFiles.AddFile("abc/donkey", null);
            var results = glob.GetResultsInFullPath(TestFiles, Helpers.DefaultTestPath);

            results.Should().BeEquivalentTo(new string[] {
                TestFiles.Path.GetFullPath("hello.txt"),
                TestFiles.Path.GetFullPath("abc/hello"),
                TestFiles.Path.GetFullPath("abc/donkey"),
            });
        }
    }
}