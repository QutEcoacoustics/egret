using Egret.Cli.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core.Tokens;
using LanguageExt;

namespace Egret.Cli.Models
{

    public class Expectation : IExpectationTest
    {

        public string Name { get; init; }


        public string Label { get; init; }

        //public string[] Labels { get; init; }

        public Bounds Bounds { get; init; }

        public Centroid Centroid { get; init; }

        //public Period? Period { get; init;}

        public bool Match { get; init; } = true;


        public Interval Duration { get; init; }
        public Interval Bandwidth { get; init; }

        public string Condition { get; init; }

        public IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents)
        {

            var conditions = new Func<Expectation, NormalizedResult, Assertion>[] {
                TestLabel
            };


            // run all relevant tests across all events storing all matches
            // the event that matches the most conditions is out candidate event
            // (i.e. the closest matching in attributes) and the one we will use
            // to form our result.
            var matches = new List<IEnumerable<Assertion>>(actualEvents.Count);
            var highestMatch = -1;
            var highestIndex = -1;
            for (int index = 0; index < actualEvents.Count; index++)
            {
                var current = actualEvents[index];
                var tests = conditions.Select(x => x(this, @current));

                var testsPassed = tests.Count(x => x is SuccessfulAssertion);
                if (testsPassed > highestMatch)
                {
                    highestMatch = testsPassed;
                    highestIndex = index;
                }
                matches.Add(tests);
            }

            // select candidate and it's results
            var candidate = actualEvents[highestIndex];
            var candidateResults = matches[highestIndex].ToArray();

            // finally form a result

            yield return new ExpectationResult(
                this,
                candidateResults
            );
        }

        public static Assertion TestLabel(Expectation expectation, NormalizedResult subject)
        {
            const string Name = "Label matches";
            return subject.Label.Match(Test, NotFound);

            Assertion Test((string Key, string Value) label)
            {
                var test = expectation.Label.Equals(label.Value, StringComparison.InvariantCultureIgnoreCase);
                if (((IExpectationTest)expectation).Matches(test))
                {
                    return new SuccessfulAssertion(Name, label.Key);
                }

                return new FailedAssertion(Name, label.Key)
                {
                    Reasons = new[]{
                        $"value `{label.Value}` ≠ expected `{expectation.Label}`"
                    }
                };
            }

            static FailedAssertion NotFound(Seq<string> reasons)
            {
                return new FailedAssertion(Name, null)
                {
                    Reasons = reasons.ToArray()
                };
            }
        }
    }
}