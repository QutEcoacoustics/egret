using Egret.Cli.Processing;
using System.Collections.Generic;
using Egret.Cli.Models.Results;
using YamlDotNet.Serialization;
using LanguageExt;

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

        /// <summary>
        /// Rough method for ordering Expectations. Some Expectations are required to run earlier or later than others.
        /// </summary>
        /// <value>A value in [0,256) representing priority. Lower values have higher priority.</value>
        [YamlIgnore]
        byte Priority { get; }

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
        byte IExpectation.Priority => 64;

        IEnumerable<ExpectationResult> Test(Option<NormalizedResult> closestEvent, IReadOnlyList<NormalizedResult> actualEvents, Suite suite);

        Validation<string, double> Distance(NormalizedResult result);
    }

    public interface ISegmentExpectation : IExpectation
    {
        IEnumerable<ExpectationResult> Test(IReadOnlyList<NormalizedResult> actualEvents, IReadOnlyList<NormalizedResult> unmatchedEvents, Suite suite);

        byte IExpectation.Priority => 128;
    }
}