using Egret.Cli.Processing;
using Egret.Cli.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core.Tokens;
using LanguageExt;
using System.Numerics;

using MoreLinq.Extensions;
using LanguageExt.SomeHelp;
using System.Collections.Immutable;
using static LanguageExt.Prelude;
using LanguageExt.UnsafeValueAccess;


namespace Egret.Cli.Models
{

    public abstract class Expectation : IExpectationTest
    {

        /// <summary>
        /// Gets the name of this event.
        /// </summary>
        /// <value></value>
        /// <remarks>
        /// Useful for aggregate expectations but not often used for
        /// expectations.
        /// </remarks>
        public abstract string Name { get; init; }

        public bool Match { get; init; } = true;

        public string Label { get; init; }

        public Interval? Duration { get; init; }
        public Interval? Bandwidth { get; init; }

        public string Condition
        {
            get => throw new NotImplementedException();
            init => throw new NotImplementedException();
        }

        /// <summary>
        /// Calculate the distance betwen `this` and the given coordinates.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public abstract Validation<string, double> Distance(NormalizedResult result);

        public abstract IEnumerable<Assertion> TestBounds(NormalizedResult result);

        public IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents)
        {
            // find closest 
            var distances = actualEvents
                .Select(Distance);
            var closest = distances
                .Index()
                .MinBy((kvp) => kvp.Value.IfFail(double.PositiveInfinity))
                .HeadOrNone();

            if (closest.IsNone)
            {
                // all results encountered errors when trying to find closest
                var allErrors = distances.Fails().ToArray();
                yield return new ExpectationResult(
                    this,
                    new ErrorAssertion("Finding closest result", null, allErrors)
                );
            }
            var (closestIndex, closestDistance) = (KeyValuePair<int, Validation<string, double>>)closest;
            var candidate = actualEvents[closestIndex];

            List<Assertion> results = new(10);

            results.Add(TestLabel(candidate));

            if (Duration is not null)
            {
                results.Add(TestBandwidth(candidate));
            }

            if (Bandwidth is not null)
            {
                results.Add(TestDuration(candidate));
            }

            results.AddRange(TestBounds(candidate));


            // // run all relevant tests across all events storing all matches
            // // the event that matches the most conditions is out candidate event
            // // (i.e. the closest matching in attributes) and the one we will use
            // // to form our result.
            // var matches = new List<IEnumerable<Assertion>>(actualEvents.Count);
            // var highestMatch = -1;
            // var highestIndex = -1;
            // for (int index = 0; index < actualEvents.Count; index++)
            // {
            //     var current = actualEvents[index];
            //     var tests = conditions.Select(x => x(this, @current));

            //     var testsPassed = tests.Count(x => x is SuccessfulAssertion);
            //     if (testsPassed > highestMatch)
            //     {
            //         highestMatch = testsPassed;
            //         highestIndex = index;
            //     }
            //     matches.Add(tests);
            // }

            // // select candidate and it's results
            // var candidate = actualEvents[highestIndex];
            // var candidateResults = matches[highestIndex].ToArray();

            // finally form a result

            yield return new ExpectationResult(
                this,
                results.ToArray()
            );
        }

        public Assertion TestLabel(NormalizedResult result)
        {
            const string Name = "Label matches";
            return result.Label.Match(Test, notFound(Name));

            Assertion Test(KeyedValue<string> label)
            {
                var test = Label.Equals(label.Value, StringComparison.InvariantCultureIgnoreCase);
                if (((IExpectationTest)this).Matches(test))
                {
                    return new SuccessfulAssertion(Name, label.Key);
                }

                return new FailedAssertion(Name, label.Key, $"value `{label.Value}` ≠ expected `{this.Label}`");
            }
        }

        public Assertion TestBandwidth(NormalizedResult result)
        {
            const string Name = "Bandwidth matches";
            return TestBound(Name, result.Bandwidth, this.Bandwidth.Value);

        }
        public Assertion TestDuration(NormalizedResult result)
        {
            const string Name = "Duration matches";
            return TestBound(Name, result.Duration, this.Duration.Value);

        }


        private static readonly Func<string, Func<Seq<string>, Assertion>> notFound = curry<string, Seq<string>, Assertion>(NotFound);

        private static ErrorAssertion NotFound(string name, Seq<string> reasons)
        {
            return new ErrorAssertion(name, null, reasons);
        }

        protected Assertion TestBound(string name, Validation<string, KeyedValue<double>> property, Interval expected)
        {
            // check we can retrieve property first
            return property.Match<Assertion>(
                Succ: prop =>
                {
                    // if we can, then test it is within expected interval
                    var (key, value) = prop;
                    var result = ((IExpectationTest)this).Matches(expected.Contains(value));
                    return result switch
                    {
                        false => new FailedAssertion(name, key, $"value `{value}` was not within {expected}"),
                        true => new SuccessfulAssertion(name, key)
                    };
                },
                Fail: errors => new ErrorAssertion(name, null, errors)
            );
        }


    }
}