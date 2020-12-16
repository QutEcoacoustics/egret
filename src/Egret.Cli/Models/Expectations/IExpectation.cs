using Egret.Cli.Processing;
using System.Collections.Generic;
using Egret.Cli.Models.Results;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{

    public interface IExpectation
    {
        string Name { get; }

        bool Match { get; init; }

        /// <summary>
        /// Does this expectation check for something is the postive sense?
        /// </summary>
        /// <example>
        /// Does an event exist? Then IsPositiveAssertion should be true.
        /// </example>
        /// <example>
        /// Are there no events? Then IsPositiveAssertion should be false. Since we're looking for a lack of something.
        /// </example>
        /// <value>True for a positive assertion.</value>
        [YamlIgnore]
        bool IsPositiveAssertion { get; }
        IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents, Suite suite);

        public bool Matches(bool test) => Match ? test : !test;
        public Contingency Result(bool test) => (Match, IsPositiveAssertion, test) switch
        {
            // this could be represented with boolean expressions
            // but I find it is easier to walk through the cases when they're 
            // written out individually
            (true, true, true) => Contingency.TruePositive,
            (false, true, true) => Contingency.FalsePositive,
            (true, true, false) => Contingency.FalseNegative,
            (false, true, false) => Contingency.TrueNegative,

            (true, false, true) => Contingency.TrueNegative,
            (false, false, true) => Contingency.FalseNegative,
            (true, false, false) => Contingency.FalsePositive,
            (false, false, false) => Contingency.TruePositive,
        };


    }

    public interface IEventExpectation : IExpectation
    {

    }

    public interface ISegmentExpectation : IExpectation
    {

    }
}