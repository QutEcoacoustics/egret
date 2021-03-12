using Egret.Cli.Processing;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using static LanguageExt.Prelude;

namespace Egret.Cli.Models
{
    public record TestCaseInclude
    {
        /// <summary>
        /// A globable string spec that is interpreted differently by different
        /// providers.
        /// It usually a file path relative to the current config file.
        /// </summary>
        /// <value>A multiglob string</value>
        public string From { get; init; }

        /// <summary>
        /// The filter to use on imported objects.
        /// Implementation is provider specific but will be understood as a multiglob
        /// </summary>
        /// <value>A multiglob string</value>
        public string Filter { get; init; }

        public MultiGlob FilterMatcher => MultiGlob.Parse(Filter.NormalizeBlank() ?? "*");

        public double? TemporalTolerance { get; init; }

        public double? SpectralTolerance { get; init; }

        public bool? Exhaustive { get; init; }

        public Override Override { get; init; }

        [YamlIgnore]
        public Arr<TestCase> Tests { get; init; } = Empty;


    }

    public record Override
    {
        public Interval? Start { get; init; }
        public Interval? End { get; init; }
        public Interval? Low { get; init; }
        public Interval? High { get; init; }
    }
}