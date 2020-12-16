
using System;
using System.Collections.Generic;
using Egret.Cli.Models.Results;

namespace Egret.Cli.Models
{
    public class EventCount : AggregateExpectation
    {
        private const string AssertionName = "Event count";
        private string name;

        public Interval Count { get; init; }
        public override bool Match { get; init; } = true;
        public override bool IsPositiveAssertion { get; } = true;

        public override string Name
        {
            get => name ?? $"Segment has {Count.ToString(simplify: true)} results";
            init => name = value;
        }


        public override IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents, Suite suite)
        {
            var success = ((IExpectation)this).Matches(Count.Contains(actualEvents.Count));

            Assertion assertion = success switch
            {
                true => new SuccessfulAssertion(AssertionName, null),
                false => new FailedAssertion(AssertionName, null, $"event count `{actualEvents.Count}` was not within {Count}")
            };

            yield return new ExpectationResult(this, assertion);

        }

    }





}