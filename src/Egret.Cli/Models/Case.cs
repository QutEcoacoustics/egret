using System;

namespace Egret.Cli.Models
{
    public class Case
    {
        public Expectation[] ExpectEvents { get; init; } = Array.Empty<Expectation>();
        public AggregateExpectation[] Expect { get; init; } = Array.Empty<AggregateExpectation>();

        public string File { get; init; }

        public Uri Uri { get; init; }

    }




}