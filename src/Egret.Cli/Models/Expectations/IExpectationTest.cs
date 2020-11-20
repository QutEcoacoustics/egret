using Egret.Cli.Processing;
using System.Collections.Generic;

namespace Egret.Cli.Models
{
    public interface IExpectationTest
    {
        string Name { get; }

        bool Match { get; init; }
        IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents);

        public bool Matches(bool test) => Match ? test : !test;
    }
}