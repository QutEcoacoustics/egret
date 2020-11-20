using Egret.Cli.Processing;
using System;
using System.Collections.Generic;

namespace Egret.Cli.Models
{
    public class EventCount : AggregateExpectation
    {
        private const string AssertionName = "Event count";
        private string name;

        public Interval Count { get; init; }
        public override bool Match { get; init; } = true;

        public override string Name
        {
            get => name ?? $"Segment has {Count} events";
            init => name = value;
        }


        public override IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents)
        {
            var success = ((IExpectationTest)this).Matches(Count.Contains(actualEvents.Count));

            Assertion assertion = success switch
            {
                true => new SuccessfulAssertion(AssertionName, null),
                false => new FailedAssertion(AssertionName, null, $"Expected {this.Count} results but {actualEvents.Count} were found")
            };

            yield return new ExpectationResult(this, assertion);

        }

    }





}