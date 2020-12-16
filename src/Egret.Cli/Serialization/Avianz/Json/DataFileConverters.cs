using Egret.Cli.Models.Avianz;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Egret.Cli.Serialization.Json.Avianz
{
    public class DataFileConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(DataFile);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return new DataFileConverter();
        }


        private class DataFileConverter : JsonConverter<DataFile>
        {
            public override DataFile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException("The root object of an AviaNZ data file must be an array");
                }

                Metadata metadata = null;
                List<Annotation> annotations = new List<Annotation>();
                var isFirstItem = true;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        return new DataFile() { Metadata = metadata, Annotations = annotations };
                    }

                    switch (isFirstItem, reader.TokenType)
                    {
                        case (true, JsonTokenType.StartObject):
                            metadata = JsonSerializer.Deserialize<Metadata>(ref reader, options);
                            break;
                        case (true, _):
                            annotations.Add(JsonSerializer.Deserialize<Annotation>(ref reader, options));
                            break;
                        case (false, JsonTokenType.StartObject):
                            throw new JsonException("AviaNZ Metadata object found after first position; that is invalid");
                        case (false, _):
                            annotations.Add(JsonSerializer.Deserialize<Annotation>(ref reader, options));
                            break;
                    }


                    if (isFirstItem)
                    {
                        isFirstItem = false;
                    }
                }

                throw new JsonException("Unexpected end of AviaNZ annotation file");
            }

            public override void Write(Utf8JsonWriter writer, DataFile value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (var item in value)
                {
                    JsonSerializer.Serialize(writer, item, options: options);
                }

                writer.WriteEndArray();
            }
        }
    }
}