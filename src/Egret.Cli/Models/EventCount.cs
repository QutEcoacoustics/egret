using Egret.Cli.Processing;
using System;
using System.Collections.Generic;

namespace Egret.Cli.Models
{
    public class EventCount : AggregateExpectation
    {

        public Interval Count { get; init; }
        public override bool Match { get; init; } = true;

        public override string Name
        {
            get
            {
                return $"Segment has {Count} events";
            }
            init => throw new System.NotImplementedException();
        }

        private const string AssertionName = "Event count";

        public override IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents)
        {
            throw new NotImplementedException();
            var success = false;// ((IExpectationTest)this).Matches(actualEvents.Count == this.Count);

            Assertion assertion = success switch
            {
                true => new SuccessfulAssertion(AssertionName, null),
                false => new FailedAssertion(AssertionName, null, $"Expected {this.Count} results but {actualEvents.Count} were found")
            };

            yield return new ExpectationResult(this, assertion);

        }

    }





}