using Egret.Cli.Models;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;

namespace Egret.Cli.Models.Results
{
    public record ExpectationResult
    {
        public ExpectationResult(IExpectation subject, params Assertion[] assertions)
        : this(subject, null, (IReadOnlyList<Assertion>)assertions)
        {
        }
        public ExpectationResult(IExpectation subject, NormalizedResult target, params Assertion[] assertions)
        : this(subject, target, (IReadOnlyList<Assertion>)assertions)
        {
        }

        public ExpectationResult(IExpectation subject,NormalizedResult target, IReadOnlyList<Assertion> assertions)
        {
            Successful = assertions.All(x => x is SuccessfulAssertion);
            Contingency = assertions.OfType<ErrorAssertion>().Any() ? None : subject.Result(Successful);
            Subject = subject;
            Target = target;
            Assertions = assertions;
        }
        public bool Successful { get; }

        public IExpectation Subject { get; init; }

        /// <summary>
        /// If provided, details the result that the subject expectation was compared against
        /// </summary>
        /// <value></value>
        public NormalizedResult Target { get; }
        public Option<Contingency> Contingency { get; init; }
        public IReadOnlyList<Assertion> Assertions { get; init; }
    }


}