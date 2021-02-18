using Egret.Cli.Serialization.Yaml;
using LanguageExt;
using static LanguageExt.Prelude;
using System;

namespace Egret.Cli.Models
{
    public record TestCase : ISourceInfo
    {
        public Arr<IExpectation> Expect { get; init; } = Empty;

        public string File { get; init; }

        public Uri Uri { get; init; } = null;
        public string Name { get; init; }


        public SourceInfo SourceInfo { get; set; }
    }
}