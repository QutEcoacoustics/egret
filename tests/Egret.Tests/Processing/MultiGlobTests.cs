using Egret.Cli.Processing;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Linq;
using Xunit;

namespace Egret.Tests.Processing
{
    public class MultiGlobTests
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
    }
}