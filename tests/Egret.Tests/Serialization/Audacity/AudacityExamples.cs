namespace Egret.Tests.Serialization.Audacity
{
    using Cli.Models;
    using Cli.Models.Audacity;
    using FluentAssertions;
    using MoreLinq;
    using Support;
    using System.IO;

    public static class AudacityExamples
    {
        public const string Example1File = @"..\..\..\..\Fixtures\Audacity\audacity-example1.aup";
        public const string Example2File = @"..\..\..\..\Fixtures\Audacity\audacity-example2.aup3";
        public const string Example2V2File = @"..\..\..\..\Fixtures\Audacity\audacity-example2.aup";

        public static readonly TestFile HostConfig = ("/abc/host.egret.yaml", @"
test_suites:
  host_suite:
    include_tests:
      - from: /abc/example1.aup
");

        public static readonly TestFile GuestConfig = (@"/abc/example1.aup", File.ReadAllText(Example1File));

        public static readonly TestFile Host3Config = ("/abc/host.egret.yaml", @"
test_suites:
  host_suite:
    include_tests:
      - from: /abc/example2.aup3
");

        // Note that this file is not actually used by the Audacity3Serializer,
        // as SqliteConnection doesn't seem to be able to use the mock file system. 
        // This is here to ensure that the file exists in the mock filesystem.
        // NOTE: the file contains placeholder data on purpose
        public static readonly TestFile Guest3Config = (@"/abc/example2.aup3", "some placeholder content");


        public static Project Example1Instance()
        {
            return new()
            {
                ProjectName = "example1_data",
                Version = "1.3.0",
                AudacityVersion = "2.4.2",
                Sel0 = 0.0,
                Sel1 = 0.0,
                SelLow = 0,
                SelHigh = 0,
                VPos = 0,
                HVal = 0.0000000000,
                Zoom = 71.6039279869,
                Rate = 44100.0,
                SnapTo = "off",
                SelectionFormat = "hh:mm:ss + milliseconds",
                FrequencyFormat = "Hz",
                BandwidthFormat = "octaves",
                SourceInfo = new SourceInfo(BuildFullPath(Example1File)),
                Tags =
                    new Tag[]
                    {
                        new() {Name = "ARTIST", Value = "artist"}, 
                        new() {Name = "TITLE", Value = "track"},
                        new() {Name = "COMMENTS", Value = "comments"}, 
                        new() {Name = "ALBUM", Value = "album"},
                        new() {Name = "YEAR", Value = "year"}, 
                        new() {Name = "TRACKNUMBER", Value = "track number"},
                        new() {Name = "GENRE", Value = "genre"},
                        new() {Name = "Custom", Value = "Custom metadata tag"},
                    },
                Tracks = new LabelTrack[]
                {
                    new()
                    {
                        Name = "Track 2",
                        IsSelected = 1,
                        Height = 206,
                        Minimized = 0,
                        NumLabels = 2,
                        Labels = new Label[]
                        {
                            new()
                            {
                                TimeStart = 4.0000000000,
                                TimeEnd = 7.2446258503,
                                SelLow = 1.0000000000,
                                SelHigh = 10.0000000000,
                                Title = "test 1"
                            },
                            new()
                            {
                                TimeStart = 15.9714285714,
                                TimeEnd = 24.4400000000,
                                SelLow = 10.0000000000,
                                SelHigh = 10000.0000000000,
                                Title = "test 3"
                            },
                        }
                    },
                    new()
                    {
                        Name = "Track 1",
                        IsSelected = 1,
                        Height = 90,
                        Minimized = 0,
                        NumLabels = 1,
                        Labels = new[]
                            {
                                new Label {TimeStart = 8.0228571429, TimeEnd = 18.9800000000, Title = "test 2"},
                            }
                    },
                },
            };
        }

        public static Project Example2Instance()
        {
            return new()
            {
                Version = "1.3.0",
                AudacityVersion = "3.0.0",
                Sel0 = 0.18857142857142856,
                Sel1 = 2.477142857142857,
                SelLow = 1230.769287109375,
                SelHigh = 7160.8388671875,
                VPos = 0,
                HVal = 0,
                Zoom = 116.66666666666667,
                Rate = 22050,
                SnapTo = "off",
                SelectionFormat = "hh:mm:ss + milliseconds",
                FrequencyFormat = "Hz",
                BandwidthFormat = "octaves",
                SourceInfo = new SourceInfo(BuildFullPath(Example1File)),
                Tags =
                    new Tag[] {new() {Name = "encoder", Value = "Lavc58.35.100 libvorbis"}},
                Tracks = new LabelTrack[]
                {
                    new()
                    {
                        Name = "Label Track",
                        IsSelected = 1,
                        Height = 73,
                        Minimized = 0,
                        NumLabels = 3,
                        Labels = new Label[]
                        {
                            new()
                            {
                                TimeStart = 0.18857142857142856,
                                TimeEnd = 2.477142857142857,
                                SelLow = 1230.769287109375,
                                SelHigh = 7160.8388671875,
                                Title = "label 3"
                            },
                            new()
                            {
                                TimeStart = 1.2257142857142858,
                                TimeEnd = 2.52,
                                SelLow = 2237.76220703125,
                                SelHigh = 5874.1259765625,
                                Title = "label 1"
                            },
                            new()
                            {
                                TimeStart = 5.854285714285714,
                                TimeEnd = 7.808571428571428,
                                SelLow = 1342.6573486328125,
                                SelHigh = 7440.5595703125,
                                Title = "label 2"
                            },
                        }
                    },
                },
            };
        }

        public static string BuildFullPath(string relativePath)
        {
            var basePath = Directory.GetCurrentDirectory();
            var relativePathNormalised = relativePath.TrimStart(Path.PathSeparator);
            return Path.GetFullPath(relativePathNormalised, basePath);
        }

        public static void Compare(Project actual, Project expected)
        {
            // TODO: compare projects
            // actual.Should().BeEquivalentTo(expected,
            //     options => options
            //         .Excluding(p => p.Tags)
            //         .Excluding(p => p.Tracks));

            actual.Tags.Should()
                .BeEquivalentTo(expected.Tags, options => options.ComparingByMembers<Tag>());

            actual.Tracks.Should().BeEquivalentTo(expected.Tracks,
                options => options.ComparingByMembers<LabelTrack>());

            actual.Tracks.ForEach((track, trackIndex) =>
            {
                track.Labels.ForEach((label, labelIndex) =>
                {
                    label.Should().BeEquivalentTo(expected.Tracks[trackIndex].Labels[labelIndex],
                        options => options.ComparingByMembers<Label>());
                });
            });
        }
    }
}