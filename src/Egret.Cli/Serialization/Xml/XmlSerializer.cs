namespace Egret.Cli.Serialization.Xml
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// The default XML serializer.
    /// </summary>
    public class DefaultXmlSerializer
    {
        /// <summary>
        /// Deserialize a file to the specified type.
        /// </summary>
        /// <param name="fileInfo">The source file.</param>
        /// <param name="onUnknownAttribute">Action to take when an unknown attribute is found.</param>
        /// <param name="onUnknownElement">Action to take when an unknown element is found.</param>
        /// <param name="onUnknownNode">Action to take when an unknown node is found.</param>
        /// <param name="onUnreferencedObject">Action to take when a known but unreferenced object is found.</param>
        /// <typeparam name="T">Deserialize to this type.</typeparam>
        /// <returns></returns>
        public async Task<T> Deserialize<T>(IFileInfo fileInfo,
            Action<object, XmlAttributeEventArgs> onUnknownAttribute = null,
            Action<object, XmlElementEventArgs> onUnknownElement = null,
            Action<object, XmlNodeEventArgs> onUnknownNode = null,
            Action<object, UnreferencedObjectEventArgs> onUnreferencedObject = null)
        {
            await using Stream stream = File.OpenRead(fileInfo.FullName);
            return Deserialize<T>(stream, onUnknownAttribute, onUnknownElement, onUnknownNode, onUnreferencedObject);
        }

        /// <summary>
        /// Deserialize a file to the specified type.
        /// </summary>
        /// <param name="stream">The source stream.</param>
        /// <param name="onUnknownAttribute">Action to take when an unknown attribute is found.</param>
        /// <param name="onUnknownElement">Action to take when an unknown element is found.</param>
        /// <param name="onUnknownNode">Action to take when an unknown node is found.</param>
        /// <param name="onUnreferencedObject">Action to take when a known but unreferenced object is found.</param>
        /// <typeparam name="T">Deserialize to this type.</typeparam>
        /// <returns></returns>
        public T Deserialize<T>(Stream stream,
            Action<object, XmlAttributeEventArgs> onUnknownAttribute = null,
            Action<object, XmlElementEventArgs> onUnknownElement = null,
            Action<object, XmlNodeEventArgs> onUnknownNode = null,
            Action<object, UnreferencedObjectEventArgs> onUnreferencedObject = null
        )
        {
            XmlSerializer serializer = new(typeof(T));
            if (onUnknownAttribute != null)
            {
                serializer.UnknownAttribute += (sender, args) => onUnknownAttribute(sender, args);
            }
            else
            {
                serializer.UnknownAttribute += (_, args) =>
                    throw new InvalidOperationException(
                        $"Unknown attribute {args.Attr} at {args.LineNumber}:{args.LinePosition}.");
            }

            if (onUnknownElement != null)
            {
                serializer.UnknownElement += (sender, args) => onUnknownElement(sender, args);
            }
            else
            {
                serializer.UnknownElement += (_, args) =>
                    throw new InvalidOperationException(
                        $"Unknown element {args.Element.Name} at {args.LineNumber}:{args.LinePosition}.");
            }

            if (onUnknownNode != null)
            {
                serializer.UnknownNode += (sender, args) => onUnknownNode(sender, args);
            }
            else
            {
                serializer.UnknownNode += (_, args) =>
                    throw new InvalidOperationException(
                        $"Unknown node {args.Name} at {args.LineNumber}:{args.LinePosition}.");
            }

            if (onUnreferencedObject != null)
            {
                serializer.UnreferencedObject += (sender, args) => onUnreferencedObject(sender, args);
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

        /// <summary>
        /// Serialize data to a file.
        /// </summary>
        /// <param name="path">The path to write the serialized data.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="xmlNamespace">The XML namespace.</param>
        /// <param name="xmlRootName">The name of the root element.</param>
        /// <param name="xmlPubId">The identifier of the document.</param>
        /// <param name="xmlDtd">The url to the dtd document.</param>
        /// <param name="xmlSubset">Internal subset declarations.</param>
        /// <typeparam name="T">The type of the data to serialize.</typeparam>
        public async void Serialize<T>(string path, T data, string xmlNamespace = null,
            string xmlRootName = null, string xmlPubId = null, string xmlDtd = null, string xmlSubset = null)
        {
            await using FileStream stream = File.OpenWrite(path);
            var settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = Encoding.UTF8,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
                Indent = true,
                OmitXmlDeclaration = false,
                CheckCharacters = true,
            };
            Serialize(stream, data, settings, xmlNamespace, xmlRootName, xmlPubId, xmlDtd, xmlSubset);
        }

        /// <summary>
        /// Serialize data to a file.
        /// </summary>
        /// <param name="stream">The stream to write the serialized data.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="settings">The XML writer settings.</param>
        /// <param name="xmlNamespace">The XML namespace.</param>
        /// <param name="xmlRootName">The name of the root element.</param>
        /// <param name="xmlPubId">The identifier of the document.</param>
        /// <param name="xmlDtd">The url to the dtd document.</param>
        /// <param name="xmlSubset">Internal subset declarations.</param>
        /// <typeparam name="T">The type of the data to serialize.</typeparam>
        public async void Serialize<T>(Stream stream, T data, XmlWriterSettings settings = null,
            string xmlNamespace = null,
            string xmlRootName = null, string xmlPubId = null, string xmlDtd = null, string xmlSubset = null)
        {
            // use the namespace if available
            var serializer = xmlNamespace == null
                ? new XmlSerializer(typeof(T))
                : new XmlSerializer(typeof(T), xmlNamespace);

            // use the settings if available
            await using var xmlWriter = settings == null
                ? XmlWriter.Create(stream)
                : XmlWriter.Create(stream, settings);

            // write the DOCTYPE if available
            if (xmlRootName != null)
            {
                await xmlWriter.WriteDocTypeAsync(xmlRootName, xmlPubId, xmlDtd, xmlSubset);
            }
            else if (xmlPubId != null || xmlDtd != null || xmlSubset != null)
            {
                throw new ArgumentException("The XML pubid, dtd, or subset all require an xml root name.");
            }

            // set the non-prefixed namespace if available
            if (xmlNamespace != null)
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("", xmlNamespace);
                serializer.Serialize(xmlWriter, data, ns);
            }
            else
            {
                serializer.Serialize(xmlWriter, data);
            }
        }
    }
}