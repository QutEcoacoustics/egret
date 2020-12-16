
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
        public static Option<ExpectationResult> ErrorIfNoResults(this IExpectation expectation, IEnumerable<NormalizedResult> actualEvents)
        {
            if (actualEvents.Any())
            {
                return None;
            }

            return new ExpectationResult(
                   expectation,
                   new FailedAssertion("At least one result exists", null, new[] { "The tool produced no results" }));
        }
    }
}