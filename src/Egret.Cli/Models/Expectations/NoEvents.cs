using Egret.Cli.Processing;
using System.Collections.Generic;
using Egret.Cli.Models.Results;

namespace Egret.Cli.Models
{
    public class NoEvents : AggregateExpectation
    {
        public override bool Match { get; init; } = true;
        public override bool IsPositiveAssertion { get; } = false;

        public override string Name { get; init; } = "Segment has no events";


        private const string AssertionName = "Event count";

        public override IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents, Suite suite)
        {
            var success = ((IExpectation)this).Matches(actualEvents.Count == 0);

            Assertion assertion = success switch
            {
                true => new SuccessfulAssertion(AssertionName, null),
                false => new FailedAssertion(AssertionName, null, $"Expected 0 results but {actualEvents.Count} were found")
            };

            yield return new ExpectationResult(this, assertion);

            // so.. if there are events, the we need to generate a false positive for each one produced!
            if (!success)
            {
                foreach (var result in actualEvents)
                {
                    var name = Match ? "Event not expected" : "Event expected";
                    var message = Match ? "Event no events but this event was produced" : "Expected an event but none were produced";
                    yield return new ExpectationResult(
                        this,
                        result,
                        new FailedAssertion(name, null, message))
                    {
                        // we are generating an event result from a segment expectation
                        IsSegmentResult = false
                    };
                }
            }

        }

    }
}