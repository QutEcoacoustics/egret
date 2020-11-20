using Egret.Cli.Processing;
using LanguageExt;
using LanguageExt.ClassInstances;
using LanguageExt.ClassInstances.Pred;
using LanguageExt.Common;
using LanguageExt.TypeClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;

namespace Egret.Cli.Models
{
    public class BoundedExpectation : Expectation
    {
        public Bounds Bounds { get; init; }
        public override string Name { get; init; } = "Event bounds";

        public override Validation<string, double> Distance(NormalizedResult result)
        {
            return result
                .Bounds
                .Map(bounds => (double)Maths.Distance.BoxDistance(Bounds.ToCoordinates(), bounds));
        }


        public override IEnumerable<Assertion> TestBounds(NormalizedResult result)
        {
            yield return TestBound("Start", result.Start, Bounds.StartSeconds);
            yield return TestBound("End", result.End, Bounds.EndSeconds);
            yield return TestBound("Low", result.Low, Bounds.LowHertz);
            yield return TestBound("High", result.High, Bounds.HighHertz);
        }

    }
}