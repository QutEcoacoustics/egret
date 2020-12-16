using Egret.Cli.Serialization.Json;
using System;
using System.Text.Json.Serialization;

namespace Egret.Cli.Models.Avianz
{
    public partial record Metadata(string Operator, string Reviewer, TimeSpan Duration) : MetadataOrAnnotation;

    public partial record Metadata
    {
        [JsonConverter(typeof(SecondsTimeSpanConverter))]
        public TimeSpan Duration { get; init; }
    }
}