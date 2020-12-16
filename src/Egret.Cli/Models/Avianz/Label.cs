using System.Text.Json.Serialization;

namespace Egret.Cli.Models.Avianz
{
    public record Label
    {
        public Label(string Filter, string Species, double Certainty, string Calltype)
        {
            this.Filter = Filter;
            this.Species = Species;
            this.Certainty = Certainty;
            this.Calltype = Calltype;
        }

        [JsonPropertyName("filter")]
        public string Filter { get; init; }

        [JsonPropertyName("species")]
        public string Species { get; init; }

        [JsonPropertyName("certainty")]
        public double Certainty { get; init; }

        [JsonPropertyName("calltype")]
        public string Calltype { get; init; }
    }
}