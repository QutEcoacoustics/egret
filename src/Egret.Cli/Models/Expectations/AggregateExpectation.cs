using Egret.Cli.Processing;
using System.Collections.Generic;
using System.Linq;
using Egret.Cli.Models.Results;

namespace Egret.Cli.Models
{
    public abstract class AggregateExpectation : ISegmentExpectation
    {
        /// <summary>
        /// Essentially a `kind` property - determines which child type to instantiate.
        /// </summary>
        /// <value></value>
        public string SegmentWith { get; init; }
        public abstract bool Match { get; init; }
        public abstract bool IsPositiveAssertion { get; }
        public abstract string Name { get; init; }
        public IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents, IReadOnlyList<NormalizedResult> unmatchedEvents, Suite suite)
            => Test(actualEvents, suite);

        public abstract IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents, Suite suite);
    }
}