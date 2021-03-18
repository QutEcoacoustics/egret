namespace Egret.Cli.Serialization.Xml
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    public class DefaultXmlSerializer
    {
        public Action<object, XmlAttributeEventArgs> OnUnknownAttribute { get; set; }

        public Action<object, XmlElementEventArgs> OnUnknownElement { get; set; }

        public Action<object, XmlNodeEventArgs> OnUnknownNode { get; set; }

        public Action<object, UnreferencedObjectEventArgs> OnUnreferencedObject { get; set; }
        
        public async Task<T> Deserialize<T>(IFileInfo fileInfo)
        {
            await using Stream stream = File.OpenRead(fileInfo.FullName);
            return Deserialize<T>(stream);
        }

        public T Deserialize<T>(Stream stream)
        {
            XmlSerializer serializer = new(typeof(T));
            if (OnUnknownAttribute != null)
            {
                serializer.UnknownAttribute += (sender, args) => OnUnknownAttribute(sender, args);
            }
            else
            {
                serializer.UnknownAttribute += (_, args) =>
                    throw new InvalidOperationException(
                        $"Unknown attribute {args.Attr} at {args.LineNumber}:{args.LinePosition}.");
            }

            if (OnUnknownElement != null)
            {
                serializer.UnknownElement += (sender, args) => OnUnknownElement(sender, args);
            }
            else
            {
                serializer.UnknownElement += (_, args) =>
                    throw new InvalidOperationException(
                        $"Unknown element {args.Element.Name} at {args.LineNumber}:{args.LinePosition}.");
            }

            if (OnUnknownNode != null)
            {
                serializer.UnknownNode += (sender, args) => OnUnknownNode(sender, args);
            }
            else
            {
                serializer.UnknownNode += (_, args) =>
                    throw new InvalidOperationException(
                        $"Unknown node {args.Name} at {args.LineNumber}:{args.LinePosition}.");
            }

            if (OnUnreferencedObject != null)
            {
                serializer.UnreferencedObject += (sender, args) => OnUnreferencedObject(sender, args);
            }
            else
            {
                serializer.UnreferencedObject += (_, args) =>
                    throw new InvalidOperationException(
                        $"Unreferenced object '{args.UnreferencedId}' {args.UnreferencedObject?.GetType().Name}.");
            }
            
            T result = (T)serializer.Deserialize(stream);
            return result;
        }

        
        public async void Serialize<T>(string path, T data)
        {
            XmlSerializer serializer = new(typeof(T));
            await using FileStream stream = File.OpenWrite(path);
            serializer.Serialize(stream, data);
        }
    }
}