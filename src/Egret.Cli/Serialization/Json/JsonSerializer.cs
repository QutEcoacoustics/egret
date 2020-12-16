using Microsoft.Extensions.Options;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Egret.Cli.Serialization.Json
{
    public class DefaultJsonSerializer
    {
        public DefaultJsonSerializer()
        {
            Options = new JsonSerializerOptions()
            {
                Converters = {
                    new SecondsTimeSpanConverter(),
                },
                PropertyNameCaseInsensitive = true,

            };
        }

        public JsonSerializerOptions Options { get; }

        public async Task<T> Deserialize<T>(string path)
        {
            using FileStream stream = File.OpenRead(path);
            var result = await JsonSerializer.DeserializeAsync<T>(stream, this.Options);

            return result;
        }


    }
}