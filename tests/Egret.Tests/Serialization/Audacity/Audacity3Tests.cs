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
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public class Audacity3Tests : TestBase
    {
        private readonly Audacity3Serializer audacity3Serializer;
        private readonly ConfigDeserializer configDeserializer;

        public static TheoryData<Project> Example2 => new() {AudacityExamples.Example2Instance()};


        public Audacity3Tests(ITestOutputHelper output) : base(output)
        {
            this.audacity3Serializer = this.Audacity3Serializer;
            this.configDeserializer = BuildConfigDeserializer();
        }

        [Theory]
        [FileData(AudacityExamples.Example2File)]
        public void TestAudacity3Deserialize(string filePath)
        {
            // arrange
            var sourceFile = (FileInfoBase)new FileInfo(filePath);
            var projectExpected = AudacityExamples.Example2Instance();

            // act 
            var projectActual = audacity3Serializer.Deserialize(sourceFile);

            // assert
            AudacityExamples.Compare(projectActual, projectExpected);
        }

        [Theory]
        [FileData(AudacityExamples.Example2File)]
        public async Task TestConfigDeserializer(string filePath)
        {
            var resolvedPath = Path.GetFullPath(filePath);
            var audioPath = Path.ChangeExtension(filePath, ".wav");
            TestFiles.AddFile(audioPath, "");

            // There are two file systems being used here,
            // because SqliteConnection doesn't seem to be able to use the mock file system.
            // The mock file system contains a placeholder file at placeholderPath so the file is found.
            // The placeholder path is changed in the host config content and the guest config path so they match the real file.
            // Then the real path is given to SqliteConnection,
            // and the real file exists and the mock file exists with placeholder content (which is not read). 
            var placeholderPath = "/abc/example2.aup3";
            var hostConfig = AudacityExamples.Host3Config;
            var newHostConfig = (hostConfig.Path, hostConfig.Contents.Replace(placeholderPath, resolvedPath));
            TestFiles.AddFile(newHostConfig);

            var guestConfig = AudacityExamples.Guest3Config;
            var newGuestConfig = (resolvedPath, guestConfig.Contents);
            TestFiles.AddFile(newGuestConfig);

            (Config config, Seq<Error> errors) = await this.configDeserializer.Deserialize(
                TestFiles.FileInfo.FromFileName(AudacityExamples.HostConfig.Path)
            );

            errors.Should().BeEmpty();
            config.Should().NotBeNull();

            config.TestSuites.Should().HaveCount(1);

            var testSuit = config.TestSuites["host_suite"];
            testSuit.IncludeTests.ToList().Should().HaveCount(1);

            var includeTests = testSuit.IncludeTests[0];
            includeTests.From.Should().Be(resolvedPath);

            var testsCases = includeTests.Tests;
            testsCases.ToList().Should().HaveCount(1);

            var expectations = testsCases[0].Expect;

            expectations[0].Should().BeOfType<BoundedExpectation>();
            expectations[1].Should().BeOfType<BoundedExpectation>();
            expectations[2].Should().BeOfType<BoundedExpectation>();

            var temporalTolerance = includeTests.TemporalTolerance ?? 0.5;
            var spectralTolerance = includeTests.SpectralTolerance ?? 0.5;

            var expected = AudacityExamples.Example2Instance();

            var expectation1 = (BoundedExpectation)expectations[0];
            expectation1.Name.Should().Be($"Audacity Label label 3 in track Label Track (0:0)");
            var expected1Label = expected.Tracks[0].Labels[0];
            expectation1.Bounds.StartSeconds.Should().Be(expected1Label.TimeStart.WithTolerance(temporalTolerance));
            expectation1.Bounds.EndSeconds.Should().Be(expected1Label.TimeEnd.WithTolerance(temporalTolerance));
            expectation1.Bounds.HighHertz.Should().Be(expected1Label.SelHigh.WithTolerance(spectralTolerance));
            expectation1.Bounds.LowHertz.Should().Be(expected1Label.SelLow.WithTolerance(spectralTolerance));

            var expectation2 = (BoundedExpectation)expectations[1];
            expectation2.Name.Should().Be($"Audacity Label label 1 in track Label Track (0:1)");
            var expected2Label = expected.Tracks[0].Labels[1];
            expectation2.Bounds.StartSeconds.Should().Be(expected2Label.TimeStart.WithTolerance(temporalTolerance));
            expectation2.Bounds.EndSeconds.Should().Be(expected2Label.TimeEnd.WithTolerance(temporalTolerance));
            expectation2.Bounds.HighHertz.Should().Be(expected2Label.SelHigh.WithTolerance(spectralTolerance));
            expectation2.Bounds.LowHertz.Should().Be(expected2Label.SelLow.WithTolerance(spectralTolerance));

            var expectation3 = (BoundedExpectation)expectations[2];
            expectation3.Name.Should().Be($"Audacity Label label 2 in track Label Track (0:2)");
            var expected3Label = expected.Tracks[0].Labels[2];
            expectation3.Bounds.StartSeconds.Should().Be(expected3Label.TimeStart.WithTolerance(temporalTolerance));
            expectation3.Bounds.EndSeconds.Should().Be(expected3Label.TimeEnd.WithTolerance(temporalTolerance));
            expectation3.Bounds.HighHertz.Should().Be(expected3Label.SelHigh.WithTolerance(spectralTolerance));
            expectation3.Bounds.LowHertz.Should().Be(expected3Label.SelLow.WithTolerance(spectralTolerance));
        }
    }
}