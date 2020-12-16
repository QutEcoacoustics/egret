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

        }

    }




}