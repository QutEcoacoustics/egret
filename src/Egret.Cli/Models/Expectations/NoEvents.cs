using Egret.Cli.Processing;
using System.Collections.Generic;

namespace Egret.Cli.Models
{
    public class NoEvents : AggregateExpectation
    {
        public override bool Match { get; init; } = true;

        public override string Name { get; init; } = "Segment has no events";


        private const string AssertionName = "Event count";

        public override IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents)
        {
            var success = ((IExpectationTest)this).Matches(actualEvents.Count == 0);

            Assertion assertion = success switch
            {
                true => new SuccessfulAssertion(AssertionName, null),
                false => new FailedAssertion(AssertionName, null, $"Expected 0 results but {actualEvents.Count} were found")
            };

            yield return new ExpectationResult(this, assertion);

        }

    }




}