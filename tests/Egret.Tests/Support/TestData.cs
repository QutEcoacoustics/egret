using Egret.Cli.Models;
using Egret.Tests.Models.AnalysisResults;
using LanguageExt;
using System.Collections.Generic;
using System.Collections.Immutable;
using static LanguageExt.Prelude;

namespace Egret.Tests.Support
{
    public static class TestData
    {
        private static DictionaryResult MakeResult(int index, double start, double end, double low, double high, string label)
        {
            return new DictionaryResult(index, new Dictionary<string, object>() {
                {"start", start},
                {"end", end},
                {"low", low},
                {"high", high},
                {"label", label},
            });
        }
        private static EventExpectation MakeExpectation(double start, double end, double low, double high, string label)
        {
            return new BoundedExpectation()
            {
                Bounds = new Bounds(
                    start.WithTolerance(0.01),
                    end.WithTolerance(0.01),
                    low.WithTolerance(10),
                    high.WithTolerance(10)),
                Label = label
            };
        }

        public static readonly IReadOnlyList<NormalizedResult> SomeResults = ImmutableArray.Create<NormalizedResult>(
            MakeResult(0, 1.0, 2.0, 3000, 4000, "a"),
            MakeResult(1, 5.0, 8.0, 3500, 5500, "b"),
            MakeResult(2, 6.0, 6.5, 300, 600, "c"),
            MakeResult(3, 15, 25, 3000, 3500, "d"),
            MakeResult(4, 20.0, 22.0, 500, 9900, "e")
        );

        public static readonly IReadOnlyList<EventExpectation> SomeExpectations = ImmutableArray.Create<EventExpectation>(
            MakeExpectation(1.0, 2.0, 3000, 4000, "a"),
            MakeExpectation(5.0, 8.0, 3500, 5500, "b"),
            MakeExpectation(6.0, 6.5, 300, 600, "c"),
            MakeExpectation(15, 25, 3000, 3500, "d"),
            MakeExpectation(20.0, 22.0, 500, 9900, "e")
        );

        public static int[][] SomeMatchesAreClosestTo = new int[][] {
            // expectation 0 is closest to result...
            new int[] { 0, 1 , 2, 3, 4 },
            new int[] { 1, 2 , 0, 3, 4 },
            new int[] { 2, 1 , 0, 3, 4 },
            new int[] { 3, 4 , 1, 2, 0 },
            new int[] { 4, 3 , 1, 2, 0 },
        };
    }
}