namespace Egret.Tests.Serialization.Audacity
{
    using Cli.Models;
    using Cli.Models.Audacity;
    using Cli.Serialization;
    using Cli.Serialization.Audacity;
    using FluentAssertions;
    using LanguageExt;
    using LanguageExt.Common;
    using Support;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class AudacityTests : TestBase
    {
        private readonly AudacitySerializer audacitySerializer;
        private readonly ConfigDeserializer configDeserializer;

        public static TheoryData<Project> Example1 => new() {AudacityExamples.Example1Instance()};

        public AudacityTests(ITestOutputHelper output) : base(output)
        {
            this.audacitySerializer = this.AudacitySerializer;
            this.configDeserializer = BuildConfigDeserializer();
        }

        [Theory]
        [FileData(AudacityExamples.Example1File)]
        public async Task TestAudacityDeserialize(string filePath)
        {
            // arrange
            var sourceFile = (FileInfoBase)new FileInfo(filePath);
            var projectExpected = AudacityExamples.Example1Instance();

            // act 
            var projectActual = await audacitySerializer.Deserialize(sourceFile);

            // assert
            AudacityExamples.Compare(projectActual, projectExpected);
        }

        [Theory]
        [MemberData(nameof(Example1))]
        public async Task TestAudacitySerialize(Project project)
        {
            // arrange
            var tempDir = Path.GetTempPath();
            var tempFileName = Path.GetRandomFileName();
            var tempFile = Path.Combine(tempDir, Path.ChangeExtension(tempFileName, "aup"));
            var expectedFile = AudacityExamples.BuildFullPath(AudacityExamples.Example1File);

            // act
            audacitySerializer.Serialize(tempFile, project);

            var projectActual = await audacitySerializer.Deserialize((FileInfoBase)new FileInfo(tempFile));
            var projectExpected = await audacitySerializer.Deserialize((FileInfoBase)new FileInfo(expectedFile));

            // assert
            tempFile.Should().EndWith(".aup");
            AudacityExamples.Compare(projectActual, projectExpected);
        }

        [Fact]
        public async Task TestConfigDeserializer()
        {
            TestFiles.AddFile("/abc/bird1.wav", "");
            TestFiles.AddFile("/abc/bird2.wav", "");
            TestFiles.AddFile("/def/bird3.wav", "");
            TestFiles.AddFile("/def/bird4.wav", "");
            TestFiles.AddFile(AudacityExamples.HostConfig);
            TestFiles.AddFile(AudacityExamples.GuestConfig);

            (Config config, Seq<Error> errors) = await this.configDeserializer.Deserialize(
                TestFiles.FileInfo.FromFileName(AudacityExamples.HostConfig.Path)
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

            var expectations = testsCases[0].Expect;

            expectations[0].Should().BeOfType<BoundedExpectation>();
            expectations[1].Should().BeOfType<BoundedExpectation>();
            expectations[2].Should().BeOfType<TemporalExpectation>();

            var temporalTolerance = includeTests.TemporalTolerance ?? 0.5;
            var spectralTolerance = includeTests.SpectralTolerance ?? 0.5;

            var expected = AudacityExamples.Example1Instance();
            
            var expectation1 = (BoundedExpectation)expectations[0];
            expectation1.Name.Should().Be($"Audacity Label test 1 in track Track 2 (0:0)");
            var expected1Label = expected.Tracks[0].Labels[0];
            expectation1.Bounds.StartSeconds.Should().Be(expected1Label.TimeStart.WithTolerance(temporalTolerance));
            expectation1.Bounds.EndSeconds.Should().Be(expected1Label.TimeEnd.WithTolerance(temporalTolerance));
            expectation1.Bounds.HighHertz.Should().Be(expected1Label.SelHigh.WithTolerance(spectralTolerance));
            expectation1.Bounds.LowHertz.Should().Be(expected1Label.SelLow.WithTolerance(spectralTolerance));
            
            var expectation2 = (BoundedExpectation)expectations[1];
            expectation2.Name.Should().Be($"Audacity Label test 3 in track Track 2 (0:1)");
            var expected2Label = expected.Tracks[0].Labels[1];
            expectation2.Bounds.StartSeconds.Should().Be(expected2Label.TimeStart.WithTolerance(temporalTolerance));
            expectation2.Bounds.EndSeconds.Should().Be(expected2Label.TimeEnd.WithTolerance(temporalTolerance));
            expectation2.Bounds.HighHertz.Should().Be(expected2Label.SelHigh.WithTolerance(spectralTolerance));
            expectation2.Bounds.LowHertz.Should().Be(expected2Label.SelLow.WithTolerance(spectralTolerance));

            var expectation3 = (TemporalExpectation)expectations[2];
            expectation3.Name.Should().Be($"Audacity Label test 2 in track Track 1 (1:0)");
            var expected3Label = expected.Tracks[1].Labels[0];
            expectation3.Time.StartSeconds.Should().Be(expected3Label.TimeStart.WithTolerance(temporalTolerance));
            expectation3.Time.EndSeconds.Should().Be(expected3Label.TimeEnd.WithTolerance(temporalTolerance));
            
            
        }
    }
}