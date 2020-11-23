using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Egret.Cli.Models
{
    public class Case
    {
        public IExpectationTest[] Expect { get; init; } = Array.Empty<AggregateExpectation>();

        public string File { get; init; }

        public Uri Uri { get; init; }

    }




}