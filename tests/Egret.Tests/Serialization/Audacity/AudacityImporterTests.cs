namespace Egret.Tests.Serialization.Audacity
{
    using Cli.Models;
    using Cli.Serialization;
    using Cli.Serialization.Audacity;
    using FluentAssertions;
    using LanguageExt;
    using LanguageExt.Common;
    using MathNet.Numerics;
    using MoreLinq;
    using Support;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class AudacityImporterTests : TestBase
    {
        private readonly AudacitySerializer audacitySerializer;
        private readonly ConfigDeserializer configDeserializer;

        private const string ExampleProjectFile1 = @"Serialization\Audacity\example1.aup";

        public AudacityImporterTests(ITestOutputHelper output) : base(output)
        {
            this.audacitySerializer = this.AudacitySerializer;
            this.configDeserializer = BuildConfigDeserializer();
        }

        [Theory]
        [FileData(ExampleProjectFile1)]
        public async Task TestAudacitySerializer(string filePath)
        {
            var result = await audacitySerializer.Deserialize((FileInfoBase) new FileInfo(filePath));

            result.Rate.Should().Be(44100.0);

            result.Tags.Should().HaveCount(8);
            result.Tags.Should().ContainInOrder(
                new Tag("ARTIST", "artist"),
                new Tag("TITLE", "track"),
                new Tag("COMMENTS", "comments"),
                new Tag("ALBUM", "album"),
                new Tag("YEAR", "year"),
                new Tag("TRACKNUMBER", "track number"),
                new Tag("GENRE", "genre"),
                new Tag("Custom", "Custom metadata tag")
            );

            result.Tracks.Should().HaveCount(2);

            result.Tracks.First().Should().Match<LabelTrack>(i =>
                i.Name == "Track 2" &&
                i.IsSelected == 1 &&
                i.Height == 206 &&
                i.Minimized == 0 &&
                i.Labels.Count == 2
            );
            result.Tracks.First().Labels.First().Should().Match<Label>(i =>
                i.Title == "test 1" &&
                i.TimeStart.AlmostEqual(4) &&
                i.TimeEnd.AlmostEqual(7.2446258503) &&
                i.SelLow.AlmostEqual(1) &&
                i.SelHigh.AlmostEqual(10)
            );
            result.Tracks.First().Labels.Skip(1).First().Should().Match<Label>(i =>
                i.Title == "test 3" &&
                i.TimeStart.AlmostEqual(15.9714285714) &&
                i.TimeEnd.AlmostEqual(24.44) &&
                i.SelLow.AlmostEqual(10) &&
                i.SelHigh.AlmostEqual(10000)
            );

            result.Tracks.Skip(1).First().Should().Match<LabelTrack>(i =>
                i.Name == "Track 1" &&
                i.IsSelected == 1 &&
                i.Height == 90 &&
                i.Minimized == 0 &&
                i.Labels.Count == 1
            );
            result.Tracks.Skip(1).First().Labels.First().Should().Match<Label>(i =>
                i.Title == "test 2" &&
                i.TimeStart.AlmostEqual(8.0228571429) &&
                i.TimeEnd.AlmostEqual(18.98) &&
                i.SelLow == 0 &&
                i.SelHigh == 0
            );
        }
        
        public static readonly TestFile HostConfig = ("/abc/host.egret.yaml", @"
test_suites:
  host_suite:
    tests:
      - file: bird*.wav
    include_tests:
      - from: /abc/example1.aup
");

        public static readonly TestFile GuestConfig = (@"/abc/example1.aup", File.ReadAllText(ExampleProjectFile1));
        
        [Fact]
        public async Task TestConfigDeserializer()
        {
            TestFiles.AddFile("/abc/bird1.wav", "");
            TestFiles.AddFile("/abc/bird2.wav", "");
            TestFiles.AddFile("/def/bird3.wav", "");
            TestFiles.AddFile("/def/bird4.wav", "");
            TestFiles.AddFile(HostConfig);
            TestFiles.AddFile(GuestConfig);
            
            (Config config, Seq<Error> errors) = await this.configDeserializer.Deserialize(
                TestFiles.FileInfo.FromFileName(HostConfig.Path)
            );

            errors.Should().BeEmpty();
            config.Should().NotBeNull();

            config.TestSuites.Should().HaveCount(1);
            
            var testSuit = config.TestSuites["host_suite"];
            testSuit.IncludeTests.ToList().Should().HaveCount(1);

            var includeTests = testSuit.IncludeTests[0];
            includeTests.From.Should().Be(@"/abc/example1.aup");

            var testsCases = includeTests.Tests;
            testsCases.ToList().Should().HaveCount(1);

            var tests = testsCases[0].Expect;

            tests[0].Should().BeOfType<BoundedExpectation>();
            tests[1].Should().BeOfType<BoundedExpectation>();
            tests[2].Should().BeOfType<TemporalExpectation>();

            var temporalTolerance = includeTests.TemporalTolerance ?? 0.5;
            var spectralTolerance = includeTests.SpectralTolerance ?? 0.5;

            tests.ForEach((e, index) =>
            {
                switch (index)
                {
                    case 0:
                        e.Name.Should().Be($"Audacity Label test 1 in track Track 2 (0:0)");
                        break;
                    case 1:
                        e.Name.Should().Be($"Audacity Label test 3 in track Track 2 (0:1)");
                        break;
                    case 2:
                        e.Name.Should().Be($"Audacity Label test 2 in track Track 1 (1:0)");
                        break;
                }

                switch (e)
                {
                    case BoundedExpectation bounded:
                        switch (index)
                        {
                            case 0:
                                bounded.Bounds.StartSeconds.Should().Be(4.0.WithTolerance(temporalTolerance));
                                bounded.Bounds.EndSeconds.Should().Be(7.2446258503.WithTolerance(temporalTolerance));
                                bounded.Bounds.HighHertz.Should().Be(10.0.WithTolerance(spectralTolerance));
                                bounded.Bounds.LowHertz.Should().Be(1.0.WithTolerance(spectralTolerance));
                                break;
                            case 1:
                                bounded.Bounds.StartSeconds.Should().Be(15.9714285714.WithTolerance(temporalTolerance));
                                bounded.Bounds.EndSeconds.Should().Be(24.44.WithTolerance(temporalTolerance));
                                bounded.Bounds.HighHertz.Should().Be(10000.0.WithTolerance(spectralTolerance));
                                bounded.Bounds.LowHertz.Should().Be(10.0.WithTolerance(spectralTolerance));
                                break;
                            default:
                                throw new XunitException();
                        }
                        break;
                    case TemporalExpectation temporal:
                        switch (index)
                        {
                            case 2:
                                temporal.Time.StartSeconds.Should().Be(8.0228571429.WithTolerance(temporalTolerance));
                                temporal.Time.EndSeconds.Should().Be(18.98.WithTolerance(temporalTolerance));
                                break;
                            default:
                                throw new XunitException();
                        }
                        break;
                }
            });

        }
    }
}