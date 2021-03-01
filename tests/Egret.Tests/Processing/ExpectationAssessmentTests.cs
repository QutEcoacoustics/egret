using Egret.Cli.Extensions;
using Egret.Cli.Models;
using Egret.Cli.Models.Results;
using Egret.Cli.Processing;
using Egret.Tests.Support;
using FluentAssertions;
using LanguageExt;
using LanguageExt.ClassInstances.Pred;
using LanguageExt.UnsafeValueAccess;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using static LanguageExt.Prelude;

namespace Egret.Tests.Models.Expectations
{
    public class ExpectationAssessmentTests : TestBase
    {
        public ExpectationAssessmentTests(ITestOutputHelper output) : base(output)
        {
        }

        public static IEnumerable<object[]> Examples()
        {
            var results = TestData.SomeResults.Subsets();
            var expectations = TestData.SomeExpectations.Subsets();

            return expectations.Cartesian(results, (a, b) => new object[] { a, b });
        }

        public static Func<NormalizedResult, int> GetResultIndex =>
             memo<NormalizedResult, int>((r) => TestData.SomeResults.Index().Find((x) => ReferenceEquals(r, x.Value)).ValueUnsafe().Key);
        public static Func<IEventExpectation, int> GetExpectationIndex =>
             memo<IEventExpectation, int>((r) => TestData.SomeExpectations.Index().Find((x) => ReferenceEquals(r, x.Value)).ValueUnsafe().Key);

        [Theory]
        [MemberData(nameof(Examples))]
        public void MatchingResultsToExpectations(
            IReadOnlyList<EventExpectation> expectations,
            IReadOnlyList<NormalizedResult> results)
        {
            // the basic principle of this test is that no matter the combination
            // of events/expectations produced, the same closest events should
            // be returned and matched.

            var measured = ExpectationAssessment.MatchEventExpectations(expectations, results);
            var matched = measured.Matched;
            var erroredExpectations = measured.ErroredExpectations;
            var unmatchedResults = measured.UnmatchedResults;

            int eNum = expectations.Count,
                rNum = results.Count,
                mNum = matched.Count,
                eeNum = erroredExpectations.Count,
                urNum = unmatchedResults.Count;



            // the sum of all three lists should never be more than the sum inputs - or else we are making duplicates
            const string distinctMessage = "{4} |{0}| should equal match |{1}| and unmatched/errored |{2}|";
            eNum.Should().Be(mNum + eeNum, distinctMessage, eNum, mNum, eeNum, "Expectations");
            rNum.Should().Be(mNum + urNum, distinctMessage, rNum, mNum, urNum, "Results ");


            Option<(int matches, int unExpectation, int unResults)> expected = (eNum, rNum) switch
            {
                // we expect no results and got none
                (0, 0) => (0, 0, 0),
                // expect no results (but we got some)
                (0, _) => (0, 0, rNum),
                // expect results (but we did not find them)
                (_, 0) => (0, eNum, 0),
                // we expect some results and we have some
                // more testing needed
                _ => None
            };

            if (expected)
            {
                (mNum, eeNum, urNum).Should().BeEquivalentTo(expected.Value());
                // test finished
                return;
            }

            if (erroredExpectations.Count > 0)
            {
                // results should only fail if we failed to match a result,
                // we shouldn't be seeing any errors processing them though
                erroredExpectations.SelectMany(e => e.Assertions).Should().AllBeOfType(typeof(FailedAssertion));
            }

            // the core essence of this test is that the index of the 
            // expectation is the same index as the results - and that's how
            //  we know they've been matched correctly.

            var consumedIndices = Lst<int>.Empty;
            foreach (var pair in matched)
            {
                // index in the original data
                var indexExpectation = GetExpectationIndex(pair.Expectation);

                // fetch expected closest result for expectation
                var expectedResultIndex = TestData
                    .SomeMatchesAreClosestTo[indexExpectation]
                    .First(
                        resultIndex => results.Select(GetResultIndex).Except(consumedIndices).Contains(resultIndex));

                var indexResult = GetResultIndex(pair.Result);

                expectedResultIndex
                    .Should()
                    .Be(
                        indexResult,
                        "For expectation ({2}) of index {0}, expected result index {3} should match actual result index {1}",
                        indexExpectation,
                        indexResult,
                        pair.Expectation,
                        expectedResultIndex);

                consumedIndices += expectedResultIndex;
            }

        }
    }




}