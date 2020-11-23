using Egret.Cli.Processing;
using System.Collections.Generic;
using System.Linq;

namespace Egret.Cli.Models
{
    public abstract class AggregateExpectation : IExpectationTest
    {
        /// <summary>
        /// Essentially a `kind` property - determines which child type to instantiate.
        /// </summary>
        /// <value></value>
        public string SegmentWith { get; init; }
        public abstract bool Match { get; init; }
        public abstract string Name { get; init; }
        public abstract IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents);
    }
}