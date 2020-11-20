using Egret.Cli.Processing;
using System.Collections.Generic;

namespace Egret.Cli.Models
{
    public abstract class AggregateExpectation : IExpectationTest
    {
        public abstract bool Match { get; init; }
        public abstract string Name { get; init; }
        public abstract IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents);
    }




}