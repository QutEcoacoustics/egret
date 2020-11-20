using Egret.Cli.Models;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Egret.Cli.Processing
{
    public record ExpectationResult
    {
        public ExpectationResult(IExpectationTest subject, params Assertion[] assertions)
        : this(subject, (IReadOnlyList<Assertion>)assertions)
        {
        }

        public ExpectationResult(IExpectationTest subject, IReadOnlyList<Assertion> assertions)
        {
            Successful = assertions.All(x => x is SuccessfulAssertion);
            Subject = subject;
            Assertions = assertions;
        }

        public bool Successful { get; }

        public IExpectationTest Subject { get; init; }
        public IReadOnlyList<Assertion> Assertions { get; init; }
    }

    public abstract record Assertion
    {
        public Assertion(string name, string matchedKey)
        {
            Name = name;
            MatchedKey = matchedKey;
        }
        public string Name { get; init; }
        public string MatchedKey { get; init; }
    }

    public record SuccessfulAssertion : Assertion
    {
        public SuccessfulAssertion(string name, string matchedKey) : base(name, matchedKey)
        {
        }
    }

    public record FailedAssertion : Assertion
    {
        public FailedAssertion(string name, string matchedKey, params string[] reasons) : base(name, matchedKey)
        {
            Reasons = reasons;
        }

        public FailedAssertion(string name, string matchedKey, Seq<string> reasons) : base(name, matchedKey)
        {
            Reasons = reasons.ToArray();
        }


        public IReadOnlyList<string> Reasons { get; }
    }

    public record ErrorAssertion : Assertion
    {
        public ErrorAssertion(string name, string matchedKey, params string[] reasons) : base(name, matchedKey)
        {
            Reasons = reasons;
        }
        public ErrorAssertion(string name, string matchedKey, Seq<string> reasons) : base(name, matchedKey)
        {
            Reasons = reasons.ToArray();
        }

        public IReadOnlyList<string> Reasons { get; }
    }

}