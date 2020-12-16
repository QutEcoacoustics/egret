using Egret.Cli.Models;
using Egret.Cli.Models.Avianz;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Egret.Cli.Serialization.Json.Avianz
{
    public class AnnotationConverter : JsonConverter<Annotation>
    {
        public override Annotation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("An AviaNZ annotation must be an array");
            }

            double start = 0, end = 0, low = 0, high = 0;
            Label[] labels = Array.Empty<Label>();
            int index = 0;
            while (reader.Read())
            {
                switch (index, reader.TokenType)
                {
                    case (0, JsonTokenType.Number):
                        start = reader.GetDouble();
                        break;
                    case (1, JsonTokenType.Number):
                        end = reader.GetDouble();
                        break;
                    case (2, JsonTokenType.Number):
                        low = reader.GetDouble();
                        break;
                    case (3, JsonTokenType.Number):
                        high = reader.GetDouble();
                        break;
                    case (4, JsonTokenType.StartArray):
                        labels = JsonSerializer.Deserialize<Label[]>(ref reader, options);
                        break;
                    case (5, JsonTokenType.EndArray):
                        return End();
                    case ( >= 5, _):
                        throw new JsonException("AviaNZ Annotation object found has more than 5 items - should only have [start, end, low, high, labels[]]");
                    default:
                        throw new JsonException($"AviaNZ Annotation is not structured properly. Did not expect {reader.TokenType} at {index}");
                }

                index++;
            }

            throw new JsonException("Unexpected end of AviaNZ annotation array");

            Annotation End()
            {
                return new Annotation(start, end, low, high, labels);
            }
        }

        public override void Write(Utf8JsonWriter writer, Annotation value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            writer.WriteNumberValue(value.Start);
            writer.WriteNumberValue(value.End);
            writer.WriteNumberValue(value.Low);
            writer.WriteNumberValue(value.High);
            JsonSerializer.Serialize(writer, value.Labels, options: options);

            writer.WriteEndArray();
        }
    }
}