using Egret.Cli.Models;
using Egret.Cli.Models.Avianz;
using Egret.Cli.Serialization.Json;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Egret.Cli.Serialization.Avianz
{
    public class AvianzDeserializer
    {
        private readonly DefaultJsonSerializer serializer;

        public AvianzDeserializer(DefaultJsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        public async Task<DataFile> DeserializeLabelFile(string path)
        {
            var result = await serializer.Deserialize<DataFile>(path);

            // set source on annotation objects
            result.SourceInfo = new SourceInfo(path);
            return result;
        }
    }
}