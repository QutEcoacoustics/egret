using Egret.Cli.Models.Results;
using System.Collections.Generic;
using System.Linq;

namespace Egret.Cli.Models.Expectations
{
    public class NoExtraResultsExpectation : IEventExpectation
    {
        private const string AssertionName = "No extra events allowed";

        public string Name => "Extra events";

        // not sure if or how we serialize this into yaml yet
        public bool Match { get => true; init => throw new System.NotImplementedException(); }

        public bool IsPositiveAssertion => false;

        public IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents, Suite suite)
        {
            // okay now we need to determine if there are any extra events specified!
            // on the asssumption that data is exhaustively labelled
            var unmatchedEvents = actualEvents.Where(x => !x.IsMarked);

            if (!unmatchedEvents.Any())
            {
                // no unmatched events, simply return
                yield break;
            }

            foreach (var result in unmatchedEvents)
            {
                var reason = "An extra event was returned from the tool. This event was not matched by any event-expectation. "
                    + "If your labels are exhaustive then this is a FP, or if not, maybe a TP.";
                yield return new ExpectationResult(
                    this,
                    result,
                    new FailedAssertion(AssertionName, null, reason)
                );
            }
        }
    }
}