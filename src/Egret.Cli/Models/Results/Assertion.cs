using Egret.Cli.Models;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Egret.Cli.Models.Results
{
    public abstract record Assertion
    {
        public Assertion(string name, string matchedKey)
        {
            Name = name;
            MatchedKey = matchedKey;
        }

        public Assertion(string name, string matchedKey, params string[] reasons) : this(name, matchedKey)
        {
            Reasons = reasons;
        }
        public Assertion(string name, string matchedKey, Seq<string> reasons) : this(name, matchedKey)
        {
            Reasons = reasons.ToArray();
        }


        public string Name { get; init; }
        public string MatchedKey { get; init; }

        public IReadOnlyList<string> Reasons { get; init; }
    }


    public record SuccessfulAssertion : Assertion
    {
        public SuccessfulAssertion(string name, string matchedKey, params string[] reasons) : base(name, matchedKey, reasons)
        {
        }

        public SuccessfulAssertion(string name, string matchedKey, Seq<string> reasons) : base(name, matchedKey, reasons)
        {
        }
    }

    public record FailedAssertion : Assertion
    {
        public FailedAssertion(string name, string matchedKey, params string[] reasons) : base(name, matchedKey, reasons)
        {
        }

        public FailedAssertion(string name, string matchedKey, Seq<string> reasons) : base(name, matchedKey, reasons)
        {
        }



    }

    public record ErrorAssertion : Assertion
    {
        public ErrorAssertion(string name, string matchedKey, params string[] reasons) : base(name, matchedKey, reasons)
        {
        }
        public ErrorAssertion(string name, string matchedKey, Seq<string> reasons) : base(name, matchedKey, reasons)
        {
        }
    }

}