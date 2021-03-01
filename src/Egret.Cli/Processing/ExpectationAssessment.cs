using LanguageExt;
using static LanguageExt.Prelude;
using Egret.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Egret.Cli.Models.Results;
using Egret.Cli.Models.Expectations;
using MoreLinq;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Diagnostics;

namespace Egret.Cli.Processing
{
    public static class ExpectationAssessment
    {

        public static List<ExpectationResult> AssessResults(IReadOnlyList<IExpectation> expectations, IReadOnlyList<NormalizedResult> actual, Suite suite)
        {
            var results = new List<ExpectationResult>(expectations.Count);


            // find event expectations
            var eventExpectations = expectations.OfType<IEventExpectation>().OrderBy(e => e.Priority).ToArray();


            // match all events to their closest results
            var measured = MatchEventExpectations(eventExpectations, actual);

            // process the matches
            foreach (var match in measured.Matched)
            {
                results.AddRange(match.Expectation.Test(match.Result, actual, suite));
            }

            // report expectations that errored while calculating distance
            // unmatched expectations (i.e. not enough results to go around) are included in this group
            results.AddRange(measured.ErroredExpectations);

            // unmatched results have been marked (flagged) and will be dealt with later
            //measured.UnmatchedResults;

            // lastly do evaluate the segment expectations
            var segmentExpectations = expectations.OfType<ISegmentExpectation>().OrderBy(e => e.Priority);
            foreach (var expectation in segmentExpectations)
            {
                results.AddRange(expectation.Test(actual, measured.UnmatchedResults.ToList(), suite));
            }

            return results;
        }

        public static MatchResults MatchEventExpectations(IReadOnlyList<IEventExpectation> expectations, IReadOnlyList<NormalizedResult> results)
        {
            // deal with some edge cases first
            if (expectations is null) { throw new ArgumentNullException(nameof(expectations)); }
            if (results is null) { throw new ArgumentNullException(nameof(results)); }


            // for each expectation
            // find closest event, pair the results
            var erroredExpectations = new Lst<ExpectationResult>();
            var unmatchedExpectations = expectations.Index().ToHashMap();

            // find distances between all expectations and all results
            var distances = DenseMatrix.Create(expectations.Count, results.Count, double.PositiveInfinity);

            for (int i = 0; i < expectations.Count; i++)
            {
                var expectation = expectations[i];

                // do some error checking. If errors are found rows are skipped and distance is infinite
                if (expectation.ErrorIfNoResults(results).Case is ExpectationResult error)
                {
                    unmatchedExpectations = unmatchedExpectations.Remove(i);
                    erroredExpectations += error;
                    continue;
                }

                // measure all distances to each result
                var rowDistances = results.Select(r => expectation.Distance(r));
                var either = expectation.ErrorIfCannotMeasureDistance(rowDistances);
                switch (either.Case)
                {
                    case ExpectationResult distanceError:
                        unmatchedExpectations = unmatchedExpectations.Remove(i);
                        erroredExpectations += distanceError;
                        break;
                    case double[] values:
                        distances.SetRow(i, rowDistances.Select(v => v.IfFail(double.PositiveInfinity)).ToArray());
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            // order distances from closest to furthest, keeping indexes
            var ordered = distances.EnumerateIndexed().OrderBy(tuple => tuple.Item3);

            var paired = new Lst<ExpectationMatch>();
            var unmatchedResults = results.Index().ToHashMap();

            // the first item is the best-matched expectation-result tuple, the last, the worst
            foreach (var (i, j, d) in ordered)
            {
                if (d == double.PositiveInfinity)
                {
                    // error case - reported already, but if we're here every other distance after this is an error
                    break;
                }

                if (unmatchedExpectations.IsEmpty || unmatchedResults.IsEmpty)
                {
                    // we've run out of expectations or results there's no reason to continue
                    break;
                }

                var expectation = unmatchedExpectations.Find(i);
                var result = unmatchedResults.Find(j);

                if (expectation.Case is IEventExpectation e && result.Case is NormalizedResult r)
                {
                    // add the match
                    paired += new ExpectationMatch(e, r);
                    unmatchedExpectations = unmatchedExpectations.Remove(i);
                    unmatchedResults = unmatchedResults.Remove(j);
                }

                // do nothing - other iterations will or will not match
            }

            // all expectations should have been matched with a result or should have generated an error
            // anything left over means there are more expectations than results
            erroredExpectations = erroredExpectations.AddRange(
                unmatchedExpectations.Values.Select(CommonExpectations.ErrorIfNotEnoughResults));

            // finally report results
            return new MatchResults(
                paired,
                erroredExpectations.ToArray(),
                unmatchedResults.Values.ToArray());

        }
    }

    public record ExpectationMatch(IEventExpectation Expectation, NormalizedResult Result);
    public record MatchResults(
        IReadOnlyCollection<ExpectationMatch> Matched,
        IReadOnlyCollection<ExpectationResult> ErroredExpectations,
        IReadOnlyCollection<NormalizedResult> UnmatchedResults);
}