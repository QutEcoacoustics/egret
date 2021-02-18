using Egret.Cli.Serialization.Json;
using System;
using System.Text.Json.Serialization;

namespace Egret.Cli.Models.Avianz
{
    public partial record Metadata : MetadataOrAnnotation
    {
        public string Operator { get; }

        public string Reviewer { get; }

        [JsonConverter(typeof(SecondsTimeSpanConverter))]
        public TimeSpan Duration { get; }

        public Metadata(string Operator, string Reviewer, TimeSpan Duration)
        {
            this.Duration = Duration;
            this.Reviewer = Reviewer;
            this.Operator = Operator;
        }
    }
}