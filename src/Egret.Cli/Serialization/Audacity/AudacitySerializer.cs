namespace Egret.Cli.Serialization.Audacity
{
    using Models;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading.Tasks;
    using Xml;

    public partial class AudacitySerializer
    {
        private readonly DefaultXmlSerializer serializer;

        public AudacitySerializer(DefaultXmlSerializer serializer)
        {
            this.serializer = serializer;
        }

        public async Task<Project> Deserialize(IFileInfo fileInfo)
        {
            var result = await serializer.Deserialize<Project>(fileInfo);
            result.SourceInfo = new SourceInfo(fileInfo.FullName);
            return result;
        }

        public Project Deserialize(Stream stream, string path)
        {
            var result = serializer.Deserialize<Project>(stream);
            result.SourceInfo = new SourceInfo(path);
            return result;
        }

        public void Serialize(string path, Project project)
        {
            serializer.Serialize(path, project);
        }
    }
}