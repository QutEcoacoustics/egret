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
using Egret.Cli.Models.Expectations;
using Egret.Cli.Models.Results;

namespace Egret.Cli.Models
{

    public abstract class Expectation : IEventExpectation
    {
        protected Expectation()
        {
        }

        protected Expectation(object context)
        {
            Context = context;
        }

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

        public bool IsPositiveAssertion { get; } = true;

        public string Label { get; init; }

        public string[] AnyLabel { get; init; } = System.Array.Empty<string>();

        public Interval? Duration { get; init; }
        public Interval? Bandwidth { get; init; }

        public Interval? Index { get; init; }

        public string Condition
        {
            get => null;
            init => throw new NotImplementedException();
        }

        /// <summary>
        /// When an expectation is loaded from an external source, we keep a reference to the original data object that
        /// was transformed into this expectation.
        /// </summary>
        /// <value></value>
        public object Context { get; init; }


        /// <summary>
        /// Calculate the distance betwen `this` and the given coordinates.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public abstract Validation<string, double> Distance(NormalizedResult result);

        public abstract IEnumerable<Assertion> TestBounds(NormalizedResult result);

        public IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents, Suite suite)
        {
            if (this.ErrorIfNoResults(actualEvents).Case is ExpectationResult error)
            {
                yield return error;
                yield break;
            }

            // find closest event that has not already been matched
            // TODO: distance measuring needs to be split into a different method otherwise it's an unfair "who matches first" system
            var filteredEvents = actualEvents.Where(e => !e.IsMarked).ToArray();

            if (!filteredEvents.Any())
            {
                // all results have been "reserved" and there are not enough results produced by the recognizer to satisfy the expectation
                yield return new ExpectationResult(
                    this,
                    new FailedAssertion(
                        "Not enough results",
                        null,
                        "There are not enough results produced to satisfy all expectations. All current results have already been matched to other expectations"
                    )
                );

                // cannot continue for this result
                yield break;
            }

            var distances = filteredEvents
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

                // cannot continue for this result
                yield break;
            }

            var (closestIndex, closestDistance) = (KeyValuePair<int, Validation<string, double>>)closest;
            var candidate = filteredEvents[closestIndex];

            List<Assertion> results = new(10);

            var labelMatched = false;
            if (Label is not null)
            {
                var labelAssertion = TestLabel(candidate, suite.LabelAliases);

                labelMatched |= labelAssertion is SuccessfulAssertion;
                results.Add(labelAssertion);
            }

            if (AnyLabel is not null)
            {
                var labelAssertion = TestAnyLabel(candidate, suite.LabelAliases);

                labelMatched |= labelAssertion is SuccessfulAssertion;
                results.Add(labelAssertion);
            }

            if (Duration is not null)
            {
                results.Add(TestBandwidth(candidate));
            }

            if (Bandwidth is not null)
            {
                results.Add(TestDuration(candidate));
            }

            if (Index is not null)
            {
                results.Add(TestIndex(candidate, closestIndex));
            }

            results.AddRange(TestBounds(candidate));

            // reserve this candidate so other events may not match it
            candidate.Mark(this);

            // finally form a result
            yield return new ExpectationResult(
                this,
                results.ToArray()
            );
        }

        public const string NameAssertionName = "Label";
        public Assertion TestLabel(NormalizedResult result, AliasedString labelAliases)
        {
            return result.Labels.Match(Test, notFound(NameAssertionName));

            Assertion Test(KeyedValue<IEnumerable<string>> labels)
            {
                var expected = labelAliases.With(Label);
                var actualString = labels.Value.JoinMoreThanOneIntoSetNotation();
                var test = expected.MatchAny(labels.Value, StringComparison.InvariantCultureIgnoreCase);

                if (((IExpectation)this).Matches(test.IsSome))
                {
                    var (first, second) = (ValueTuple<string, string>)test;
                    return new SuccessfulAssertion(NameAssertionName, labels.Key, $"`{first}` = `{second}`");
                }

                return new FailedAssertion(NameAssertionName, labels.Key, $"value `{labels.Value.JoinMoreThanOneIntoSetNotation()}` ≠ expected `{expected}` expected");
            }
        }

        public const string AnyLabelAssertionName = "Any label";
        public Assertion TestAnyLabel(NormalizedResult result, AliasedString labelAliases)
        {
            return result.Labels.Match(Test, notFound(AnyLabelAssertionName));


            Assertion Test(KeyedValue<IEnumerable<string>> labels)
            {
                var expected = labelAliases.With(AnyLabel);
                var test = expected.MatchAny(labels.Value, StringComparison.InvariantCultureIgnoreCase);
                if (((IExpectation)this).Matches(test.IsSome))
                {
                    return new SuccessfulAssertion(AnyLabelAssertionName, labels.Key);
                }

                return new FailedAssertion(AnyLabelAssertionName, labels.Key, $"value `{labels.Value.JoinMoreThanOneIntoSetNotation()}` ∉ `{expected}` expected");
            }
        }

        public Assertion TestBandwidth(NormalizedResult result)
        {
            const string Name = "Bandwidth";
            return TestBound(Name, result.Bandwidth, Bandwidth.Value);

        }
        public Assertion TestDuration(NormalizedResult result)
        {
            const string Name = "Duration";
            return TestBound(Name, result.Duration, Duration.Value);

        }
        public Assertion TestIndex(NormalizedResult _, int resultIndex)
        {
            const string Name = "Index";
            return TestBound(Name, new KeyedValue<double>("Index", resultIndex), Index.Value);

        }

        internal static readonly Func<string, Func<Seq<string>, Assertion>> notFound = curry<string, Seq<string>, Assertion>(NotFound);

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
                    var result = ((IExpectation)this).Matches(expected.Contains(value));
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