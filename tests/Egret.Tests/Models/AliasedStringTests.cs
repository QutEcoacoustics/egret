using Egret.Cli.Models;
using Egret.Tests.Support;
using FluentAssertions;
using LanguageExt;
using System;
using Xunit;
using Xunit.Abstractions;
using static LanguageExt.Prelude;
using static System.StringComparison;

namespace Egret.Tests.Models
{
    public class AliasedStringTests : TestBase
    {
        private static readonly AliasedString Aliases = new("ABC", "DEF", "HIJ");

        public AliasedStringTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void NullIsRejected()
        {
            new AliasedString(null).Should().BeEmpty();
            Action bad = () => new AliasedString("abc", null);

            bad.Should().Throw<ArgumentNullException>().WithMessage("*aliases*");

        }

        [Theory]
        [InlineData("ABC", true)]
        [InlineData("abc", false)]
        [InlineData("DEF", true)]
        [InlineData("HIJ", true)]
        [InlineData("monkey", false)]
        public void AliasedStringEquality(string test, bool match)
        {
            (test == Aliases).Should().Be(match);
        }

        [Theory]
        [InlineData("ABC", InvariantCulture, "ABC")]
        [InlineData("abc", InvariantCultureIgnoreCase, "ABC")]
        [InlineData("DEF", InvariantCultureIgnoreCase, "DEF")]
        [InlineData("HIJ", InvariantCultureIgnoreCase, "HIJ")]
        [InlineData("hij", InvariantCulture, null)]
        [InlineData("", InvariantCulture, null)]
        [InlineData("monkey", InvariantCulture, null)]
        [InlineData("null", InvariantCulture, null)]
        public void AliasedStringMatch(string test, StringComparison comparison, string match)
        {
            var result = Aliases.Match(test, comparison);

            Assert.Equal(Optional(match), result);
        }

        public static TheoryData<Seq<string>, StringComparison, Option<(string MatchedAlias, string MatchedOther)>> Examples()
        {
            return new TheoryData<Seq<string>, StringComparison, Option<(string MatchedAlias, string MatchedOther)>>() {
                 { Empty, default, None },
                { Seq1("ABC"), default, Some(("ABC", "ABC")) },
                { Seq1("abc"), default, None },
                { Seq1("abc"), InvariantCultureIgnoreCase, Some(("ABC", "abc")) },
                { Seq1("hij"), InvariantCultureIgnoreCase, Some(("HIJ", "hij")) },
                { Seq("bannanna", "monkey", "ABC"), default, Some(("ABC", "ABC")) },
                { Seq("bannanna", "monkey", "abc"), InvariantCultureIgnoreCase, Some(("ABC", "abc")) },
                { Seq("bannanna", "monkey", "rabbit"), default, None },
            };
        }

        [Theory]
        [MemberData(nameof(Examples))]
        public void AliasedStringMatchesAny(Seq<string> Input, StringComparison Comparison, Option<(string MatchedAlias, string MatchedOther)> Expected)
        {
            var actual = Aliases.MatchAny(Input, Comparison);
            Assert.Equal(Expected, actual);
        }

        [Fact]
        public void AliasCanBeExtended()
        {
            var extended = Aliases.With("Hoot Hoot");

            Assert.Equal(Some("Hoot Hoot"), extended.Match("Hoot Hoot"));
            Assert.Equal(None, extended.Match("hoot hoot"));
            Assert.Equal(Some("ABC"), extended.Match("ABC"));

            // original should not match
            Assert.Equal(None, Aliases.Match("Hoot Hoot"));


            Assert.Equal(Some(("Hoot Hoot", "hoot hoot")), extended.MatchAny(Seq1("hoot hoot"), InvariantCultureIgnoreCase));
            Assert.Equal(None, extended.MatchAny(Seq1("hoot hoot")));
            Assert.Equal(Some(("ABC", "ABC")), extended.MatchAny(Seq1("ABC")));

        }


        [Fact]
        public void AliasCanBeDserialized()
        {
            // the default yaml deserializer should be able to deserialize this
            // this type without any help or extra configuration
            var configDeserializer = BuildConfigDeserializer();
            var deserializer = configDeserializer.BuildDeserializer("/kiss.yml");
            var result = deserializer.Deserialize<CarlyRaeJepsen>("aliases: ['call','me','maybe']");

            result.Aliases.Should().ContainInOrder(new AliasedString("call", "me", "maybe"));
        }

        public class CarlyRaeJepsen
        {
            public AliasedString Aliases { get; set; }
        }
    }
}