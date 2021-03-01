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
using Egret.Cli.Models.Results;

namespace Egret.Cli.Models
{
    public class LabelOnlyExpectation : EventExpectation
    {
        public LabelOnlyExpectation() : base()
        {
        }

        public LabelOnlyExpectation(object context) : base(context)
        {
        }

        public override string Name { get; init; } = "Label only event";

        public override Validation<string, double> Distance(NormalizedResult result)
        {
            // TODO: this is almost certainly wrong
            // Fake a "far distance" so label only expectations have lower priority than expectations with bounds
            return double.MaxValue;
        }

        public override IEnumerable<Assertion> TestBounds(NormalizedResult result)
        {
            yield break;
        }
    }
}