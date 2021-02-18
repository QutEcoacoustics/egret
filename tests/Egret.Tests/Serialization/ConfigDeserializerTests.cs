namespace Egret.Tests.Serialization
{
    using FluentAssertions;
    using global::Egret.Cli.Models;
    using global::Egret.Cli.Serialization;
    using global::Egret.Tests.Support;
    using LanguageExt;
    using LanguageExt.Common;
    using LanguageExt.DataTypes.Serialisation;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;

    public class ConfigDeserializerTests : TestBase
    {
        public ConfigDeserializerTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData("A", "A")]
        [InlineData("A,A,A", "A")]
        [InlineData("A,B,B,A", "A,B")]
        [InlineData("A,B,B,A,C,D,E,F", "A,B,C,D,E,F")]
        [InlineData("!A", "!A")]
        [InlineData("!A,B,B,!A", "!A,B")]
        [InlineData("!A,!B,!B,!A", "!A,!B")]
        public void AutomaticSegmentLabelExpectationsAreGenerated(string eventLabels, string segmentLabels)
        {
            var original = MakeFromSpec(eventLabels, segment: false);
            var testCase = new TestCase()
            {
                Expect = original
            };

            var expected = original + MakeFromSpec(segmentLabels, segment: true);


            var actual = ConfigDeserializer.GenerateAutoLabelPresenceSegmentTest(testCase).Expect;

            actual.Should<Arr<IExpectation>>().BeEquivalentTo(expected);
        }


        private static Arr<IExpectation> MakeFromSpec(string spec, bool segment)
        {
            var items = spec
                    .Split(",")
                    .Select<string, IExpectation>(
                        label => segment
                            ? new LabelPresent() { Label = label.Replace("!", ""), Match = !label.StartsWith("!") }
                            : new LabelOnlyExpectation() { Label = label.Replace("!", ""), Match = !label.StartsWith("!") }
                    );
            return Arr.createRange(items);
        }

        public static readonly TestFile HostConfig = ("/abc/host.egret.yaml", @"
test_suites:
  host_suite:
    tests:
      - file: bird*.wav
    include_tests:
      - from: /def/guest.egret.yaml
");
        public static readonly TestFile GuestConfig = ("/def/guest.egret.yaml", @"
test_suites:
  guest_suite:
    tests:
      - file: bird*.wav
");

        [Fact]
        public async void FileGlobsAreExpandedAgainstTheirSourceInfo()
        {
            // in the case of an egret configuration file (the host)
            // importing another config file (the guest)
            // for a test from the host
            //   we want the test's glob to expand against the hosts config directory
            // for a test from the guest
            //   we want the test's glob to expand against the child config directory

            TestFiles.AddFile("/abc/bird1.wav", "");
            TestFiles.AddFile("/abc/bird2.wav", "");
            TestFiles.AddFile("/def/bird3.wav", "");
            TestFiles.AddFile("/def/bird4.wav", "");
            TestFiles.AddFile(HostConfig);
            TestFiles.AddFile(GuestConfig);

            var deserializer = BuildConfigDeserializer();

            var (config, errors) = await deserializer.Deserialize(
                TestFiles.FileInfo.FromFileName(HostConfig.Path)
            );

            errors.Should().BeEmpty();

            var tests = config.TestSuites["host_suite"].Tests.AsEnumerable();
            var expectedSource = new SourceInfo(ResolvePath(HostConfig.Path), 5, 9, 6, 5);
            tests.Should().BeEquivalentTo(new[]{
                new TestCase(){ Name = "#1", File = ResolvePath("/abc/bird1.wav"), SourceInfo = expectedSource },
                new TestCase(){ Name = "#2", File = ResolvePath("/abc/bird2.wav"), SourceInfo = expectedSource }
            });

            var includeTests = config.TestSuites["host_suite"].IncludeTests.SelectMany(x => x.Tests);
            var expectedIncludeSource = new SourceInfo(ResolvePath(GuestConfig.Path), 5, 9, 6, 1);
            includeTests.Should().BeEquivalentTo(new[]{
                new TestCase(){ Name = "#1", File = ResolvePath("/def/bird3.wav"), SourceInfo = expectedIncludeSource },
                new TestCase(){ Name = "#2", File = ResolvePath("/def/bird4.wav"), SourceInfo = expectedIncludeSource }
            });

        }
    }
}