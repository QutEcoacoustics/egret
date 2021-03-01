
using Egret.Cli.Processing;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;
using Egret.Cli.Models.Results;

namespace Egret.Cli.Models.Expectations
{
    public static class CommonExpectations
    {
        public static Option<ExpectationResult> ErrorIfNoResults(
            this IEventExpectation expectation,
            IEnumerable<NormalizedResult> actualEvents)
        {
            if (actualEvents.Any())
            {
                return None;
            }

            return new ExpectationResult(
                   expectation,
                   new FailedAssertion("At least one result exists", null, new[] { "The tool produced no results" }));
        }


        public static Either<ExpectationResult, double[]> ErrorIfCannotMeasureDistance(
            this IEventExpectation expectation,
            IEnumerable<Validation<string, double>> values)
        {
            if (values.Any(v => v.IsSuccess))
            {
                return values.Select(x => x.IfFail(double.PositiveInfinity)).ToArray();
            }

            // all results encountered errors when trying to find closest
            var allErrors = values.Fails().ToArray();

            return new ExpectationResult(
                expectation,
                new ErrorAssertion("Finding closest result", null, allErrors)
            );
        }

        public static ExpectationResult ErrorIfNotEnoughResults(
                 this IEventExpectation expectation
        )
        {
            // all results have been "reserved" and there are not enough results produced by the recognizer to satisfy the expectation
            return new ExpectationResult(
                expectation,
                new FailedAssertion(
                    "Not enough results",
                    null,
                    "Not enough results were produced to match to all expectations."
                )
            );
        }
    }
}