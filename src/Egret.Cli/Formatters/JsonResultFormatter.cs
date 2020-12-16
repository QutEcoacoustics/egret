using Egret.Cli.Processing;

using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using Egret.Cli.Commands;
using Egret.Cli.Extensions;
using Egret.Cli.Serialization.Json;
using Egret.Cli.Models.Results;


namespace Egret.Cli.Formatters
{
    public class JsonResultFormatter : IResultFormatter
    {
        public const string ResultsKey = "Results";
        public const string SummaryKey = "Summary";

        private readonly FileInfo output;

        private readonly JsonWriterOptions jsonWriterOptions;
        private readonly JsonSerializerOptions serializerOptions;
        private readonly Utf8JsonWriter writer;

        public JsonResultFormatter(TestCommandOptions options)
        {
            var outFilename = options.Configuration.Filestem() + "_results.json";

            output = options.Output.Combine(outFilename);
            jsonWriterOptions = new JsonWriterOptions() { Indented = true };
            serializerOptions = new JsonSerializerOptions()
            {
                Converters = {
                    new SecondsTimeSpanConverter()
                },
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };
            writer = new Utf8JsonWriter(output.Open(FileMode.Create, FileAccess.Write, FileShare.Read), jsonWriterOptions);
        }

        public FileInfo Output => output;

        public async ValueTask DisposeAsync()
        {
            await writer.DisposeAsync();
        }

        public async ValueTask WriteResult(int index, TestCaseResult result)
        {
            JsonSerializer.Serialize(writer, result, options: serializerOptions);
            await writer.FlushAsync();
        }

        public async ValueTask WriteResultsFooter(FinalResults finalResults)
        {
            writer.WriteEndArray();
            writer.WritePropertyName(SummaryKey);
            JsonSerializer.Serialize(writer, finalResults, options: serializerOptions);
            writer.WriteEndObject();

            await writer.FlushAsync();
        }

        public async ValueTask WriteResultsHeader()
        {
            writer.WriteStartObject();
            writer.WritePropertyName(ResultsKey);
            writer.WriteStartArray();

            await writer.FlushAsync();
        }
    }
}