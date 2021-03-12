using Egret.Cli.Models.Results;
using System.Collections.Generic;
using System.Linq;

namespace Egret.Cli.Models.Expectations
{
    public class NoExtraResultsExpectation : ISegmentExpectation
    {
        private const string AssertionName = "No extra events allowed";

        public string Name => "Extra events";

        /// <summary>
        /// Whether or not this expectation should match.
        /// In this case, if the value is <c>false</c> the expectation is skipped.
        /// </summary>
        /// <value><c>true</c> if extra events should generate an error.</value>
        public bool Match { get; init; }

        public bool IsPositiveAssertion => false;

        public byte Priority => byte.MaxValue;

        public IEnumerable<ExpectationResult> Test(
            IReadOnlyList<NormalizedResult> actualEvents,
            IReadOnlyList<NormalizedResult> unmatchedEvents,
            Suite suite)
        {
            if (Match is false)
            {
                yield break;
            }

            // okay now we need to determine if there are any extra events specified!
            // on the asssumption that data is exhaustively labelled
            if (!unmatchedEvents.Any())
            {
                // no unmatched events, simply return
                yield break;
            }

            foreach (var result in unmatchedEvents)
            {
                var reason = "This event was not matched by any event-expectation. "
                    + "If your labels are exhaustive then this is a FP, or if not, maybe a TP.";
                yield return new ExpectationResult(
                    this,
                    result,
                    new FailedAssertion(AssertionName, null, reason)
                )
                { IsSegmentResult = false };
            }
        }
    }
}