using Egret.Cli.Serialization.Yaml;
using System;

namespace Egret.Cli.Models
{
    public record TestCase : ISourceInfo
    {
        public IExpectation[] Expect { get; init; } = Array.Empty<IExpectation>();

        public string File { get; init; }

        public Uri Uri { get; init; }
        public string Name { get; init; }


        public SourceInfo SourceInfo { get; set; }
    }
}