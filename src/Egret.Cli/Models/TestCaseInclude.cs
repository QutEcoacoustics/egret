using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    public record TestCaseInclude
    {
        /// <summary>
        /// A globable string spec that is interpreted differently by different
        /// providers.
        /// It usually a file spec relative to the current config file.
        /// </summary>
        /// <value></value>
        public string From { get; init; }

        /// <summary>
        /// The filter to use on imported objects.
        /// Implementation is provider specific.
        /// </summary>
        /// <value>A filter object containing either Include or Excludes</value>
        public Filter Filter { get; init; }

        public double? TemporalTolerance { get; init; }
        public double? SpectralTolerance { get; init; }

        [YamlIgnore]
        public TestCase[] Tests { get; init; } = Array.Empty<TestCase>();


    }

    public record Filter(string Include, string Exclude);
}