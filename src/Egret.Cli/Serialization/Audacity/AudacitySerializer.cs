namespace Egret.Cli.Serialization.Audacity
{
    using Microsoft.Extensions.Logging;
    using Models;
    using Models.Audacity;
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using Xml;

    public class AudacitySerializer
    {
        private readonly ILogger<AudacitySerializer> logger;
        private readonly DefaultXmlSerializer serializer;

        public static string XmlDtd = "http://audacity.sourceforge.net/xml/audacityproject-1.3.0.dtd";
        public static string XmlName = "project";
        public static string XmlNs = "http://audacity.sourceforge.net/xml/";
        public static string XmlPubId = "-//audacityproject-1.3.0//DTD//EN";

        public AudacitySerializer(ILogger<AudacitySerializer> logger, DefaultXmlSerializer serializer)
        {
            this.logger = logger;
            this.serializer = serializer;
        }

        public async Task<Project> Deserialize(IFileInfo fileInfo)
        {
            Project result = await serializer.Deserialize<Project>(fileInfo,
                (sender, args) => LogDeserializerIssue(sender, args, fileInfo.FullName),
                (sender, args) => LogDeserializerIssue(sender, args, fileInfo.FullName),
                (sender, args) => LogDeserializerIssue(sender, args, fileInfo.FullName),
                (sender, args) => LogDeserializerIssue(sender, args, fileInfo.FullName)
            );
            result.SourceInfo = new SourceInfo(fileInfo.FullName);
            return result;
        }

        public Project Deserialize(Stream stream, string path)
        {
            Project result = serializer.Deserialize<Project>(stream,
                (sender, args) => LogDeserializerIssue(sender, args, path),
                (sender, args) => LogDeserializerIssue(sender, args, path),
                (sender, args) => LogDeserializerIssue(sender, args, path),
                (sender, args) => LogDeserializerIssue(sender, args, path)
            );
            result.SourceInfo = new SourceInfo(path);
            return result;
        }

        public void Serialize(string path, Project project)
        {
            serializer.Serialize(path, project, XmlNs,
                XmlName, XmlPubId, XmlDtd);
            
            // create an empty folder so Audacity can open the .aup file
            Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(path), project.ProjectName));
        }

        private void LogDeserializerIssue(object sender, EventArgs args, string path)
        {
            switch (args)
            {
                case XmlAttributeEventArgs a:
                    logger.LogWarning(
                        $"Sender '{sender}' found an unknown XML attribute '{a.Attr.Name}' " +
                        $"in '{path}' at '{a.LineNumber}:{a.LinePosition}'.");
                    break;
                case XmlElementEventArgs a:
                    logger.LogWarning(
                        $"Sender '{sender}' found an unknown XML element '{a.Element.Name}' " +
                        $"in '{path}' at '{a.LineNumber}:{a.LinePosition}'.");
                    break;
                case XmlNodeEventArgs a:
                    logger.LogWarning(
                        $"Sender '{sender}' found an unknown XML node '{a.Name}' " +
                        $"in '{path}' at '{a.LineNumber}:{a.LinePosition}'.");
                    break;
                case UnreferencedObjectEventArgs a:
                    logger.LogWarning(
                        $"Found an unreferenced object '{a.UnreferencedId}' " +
                        $"{a.UnreferencedObject?.GetType().Name} in '{path}'.");
                    break;
                default:
                    logger.LogWarning($"Found an unknown issue in '{path}'.");
                    break;
            }
        }
    }
}