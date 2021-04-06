namespace Egret.Cli.Serialization.Audacity
{
    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Logging;
    using Models.Audacity;
    using MoreLinq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// A class to deserialize Audacity 3 Project files.
    /// </summary>
    /// <remarks>
    /// NOTE: Serialization is not implemented.
    /// Serialize to an Audacity 2 Project file instead.
    /// </remarks>
    public class Audacity3Serializer
    {
        private readonly ILogger<Audacity3Serializer> logger;
        private readonly AudacitySerializer audacitySerializer;

        public Audacity3Serializer(ILogger<Audacity3Serializer> logger, AudacitySerializer audacitySerializer)
        {
            this.logger = logger;
            this.audacitySerializer = audacitySerializer;
        }

        public Project Deserialize(IFileInfo fileInfo)
        {
            var info = GetAudacityFormatInfo(fileInfo);
            var project = GetProject(fileInfo);

            return project;
        }

        private string BuildConnectionString(IFileInfo fileInfo)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                Mode = SqliteOpenMode.ReadWriteCreate, DataSource = fileInfo.FullName,
            };
            return connectionString.ToString();
        }

        private void ExecuteReader(IFileInfo fileInfo, string commandText,
            Dictionary<string, object> parameters = null,
            Action<SqliteConnection, SqliteCommand, SqliteDataReader> action = null)
        {
            var connectionString = this.BuildConnectionString(fileInfo);
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = commandText;
            parameters?.ForEach(pair => command.Parameters.AddWithValue(pair.Key, pair.Value));

            using var reader = command.ExecuteReader();
            action?.Invoke(connection, command, reader);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <remarks>
        /// Implemented from reverse engineering and learning from code at
        /// https://github.com/audacity/audacity/blob/2c47dc5ba1a91c87a457ffba6caf8b1baf706279/src/ProjectFileIO.cpp
        /// </remarks>
        /// <returns></returns>
        private Dictionary<string, string> GetAudacityFormatInfo(IFileInfo fileInfo)
        {
            string applicationIdRaw = null;
            string userVersionRaw = null;

            ExecuteReader(fileInfo, "PRAGMA application_id; PRAGMA user_version;", action: (conn, comm, reader) =>
            {
                while (reader.Read())
                {
                    applicationIdRaw = reader.GetString(0);
                }

                reader.NextResult();
                while (reader.Read())
                {
                    userVersionRaw = reader.GetString(0);
                }
            });

            var applicationIdInt = Convert.ToInt32(applicationIdRaw);
            var applicationIdBytes = BitConverter.GetBytes(applicationIdInt);
            var applicationIdString = Encoding.UTF8.GetString(applicationIdBytes).Reverse();
            var applicationId = string.Join("", applicationIdString);

            var userVersionInt = Convert.ToInt32(userVersionRaw);
            var userVersionBytes = BitConverter.GetBytes(userVersionInt);
            var userVersionNumbers = userVersionBytes.Reverse();
            var userVersion = string.Join(".", userVersionNumbers);

            return new Dictionary<string, string> {{"applicationId", applicationId}, {"userVersion", userVersion},};
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Implemented from reverse engineering and learning from code at
        /// https://github.com/audacity/audacity/blob/2c47dc5ba1a91c87a457ffba6caf8b1baf706279/src/ProjectFileIO.cpp
        /// and
        /// https://github.com/audacity/audacity/blob/2c47dc5ba1a91c87a457ffba6caf8b1baf706279/src/ProjectSerializer.cpp
        /// </remarks>
        /// <returns></returns>
        private Project GetProject(IFileInfo fileInfo)
        {
            var culture = CultureInfo.InvariantCulture;
            using var scope = logger.BeginScope("Processing Audacity 3 project file {path}", fileInfo.Name);
            Project project = null;
            ExecuteReader(fileInfo, "SELECT dict, doc FROM project WHERE id = 1;", action: (conn, comm, reader) =>
            {
                while (reader.Read())
                {
                    if (project != null)
                    {
                        throw new InvalidOperationException("Audacity 3 project has already been loaded.");
                    }

                    // read the dict and doc from the sqlite file
                    var bufferDict = (byte[])reader.GetValue(0);
                    var bufferDoc = (byte[])reader.GetValue(1);
                    var buffer = bufferDict.Concat(bufferDoc).ToArray();

                    // create a memory stream and binary reader as each have benefits when reading the buffer
                    using var inStream = new MemoryStream(buffer);
                    using var inBinary = new BinaryReader(inStream);

                    // create the xml stream to write to
                    using var contentWriter = new StringWriter();
                    var settings = new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8, Indent = true, OmitXmlDeclaration = true
                    };
                    using var contentXml = XmlWriter.Create(contentWriter, settings);

                    // implement the "stack of dictionaries" used in the custom XML serialization
                    var currentIds = new Dictionary<ushort, string>();
                    var allIds = new Stack<Dictionary<ushort, string>>();

                    // need an initial encoding
                    Encoding encoding = Encoding.Default;

                    // read the first value, which is a value indicating the type of data stored in the current 'block'
                    int currentValue = inBinary.Read();

                    // read the data in a loop - first the dict of xml keys and values, then the xml doc
                    while (currentValue > -1)
                    {
                        ushort id;
                        int count;
                        string name;
                        string text;

                        switch (currentValue)
                        {
                            case (int)FieldTypes.Push:
                                allIds.Push(currentIds);
                                currentIds = new Dictionary<ushort, string>();
                                logger.LogTrace("Pushed new id dictionary on to stack.");
                                break;

                            case (int)FieldTypes.Pop:
                                currentIds = allIds.Pop();
                                logger.LogTrace("Popped id dictionary from stack.");
                                break;

                            case (int)FieldTypes.Name:
                                id = inBinary.ReadUInt16();
                                count = inBinary.ReadUInt16();
                                text = ReadString(inBinary, count, encoding);
                                currentIds[id] = text;
                                logger.LogTrace($"Added {id}={text} to dictionary.");
                                break;

                            case (int)FieldTypes.StartTag:
                                id = inBinary.ReadUInt16();
                                name = GetName(currentIds, id);
                                contentXml.WriteStartElement(name, AudacitySerializer.XmlNs);
                                logger.LogTrace($"Wrote tag start {name}.");
                                break;

                            case (int)FieldTypes.EndTag:
                                id = inBinary.ReadUInt16();
                                name = GetName(currentIds, id);
                                contentXml.WriteEndElement();
                                logger.LogTrace($"Wrote tag end {name}.");
                                break;

                            case (int)FieldTypes.String:
                                id = inBinary.ReadUInt16();
                                count = inBinary.ReadInt32();
                                name = GetName(currentIds, id);
                                text = ReadString(inBinary, count, encoding);
                                contentXml.WriteAttributeString(name, text);
                                logger.LogTrace($"Wrote attribute string {name}={text}.");
                                break;

                            case (int)FieldTypes.Float:
                                id = inBinary.ReadUInt16();
                                var floatValue = inBinary.ReadSingle();
                                var floatDigits = inBinary.ReadInt32();
                                name = GetName(currentIds, id);
                                contentXml.WriteAttributeString(name, floatValue.ToString(culture));
                                logger.LogTrace($"Wrote attribute float {name}={floatValue}.");
                                break;

                            case (int)FieldTypes.Double:
                                id = inBinary.ReadUInt16();
                                var doubleValue = inBinary.ReadDouble();
                                var doubleDigits = inBinary.ReadInt32();
                                name = GetName(currentIds, id);
                                contentXml.WriteAttributeString(name, doubleValue.ToString(culture));
                                logger.LogTrace($"Wrote attribute double {name}={doubleValue}.");
                                break;

                            case (int)FieldTypes.Int:
                                id = inBinary.ReadUInt16();
                                var intValue = inBinary.ReadUInt32();
                                name = GetName(currentIds, id);
                                contentXml.WriteAttributeString(name, intValue.ToString(culture));
                                logger.LogTrace($"Wrote attribute int {name}={intValue}.");
                                break;

                            case (int)FieldTypes.Bool:
                                id = inBinary.ReadUInt16();
                                var boolValue = inBinary.ReadBoolean();
                                name = GetName(currentIds, id);
                                contentXml.WriteAttributeString(name, boolValue ? "1" : "0");
                                logger.LogTrace($"Wrote attribute bool {name}={boolValue}.");
                                break;

                            case (int)FieldTypes.Long:
                                id = inBinary.ReadUInt16();
                                var longValue = inBinary.ReadInt32();
                                name = GetName(currentIds, id);
                                contentXml.WriteAttributeString(name, longValue.ToString(culture));
                                logger.LogTrace($"Wrote attribute long {name}={longValue}.");
                                break;

                            case (int)FieldTypes.LongLong:
                                id = inBinary.ReadUInt16();
                                // check C++ to C# type equivalence:
                                // https://www.tangiblesoftwaresolutions.com/articles/csharp_equivalent_to_cplus_types.html
                                var longLongValue = inBinary.ReadInt64();
                                name = GetName(currentIds, id);
                                contentXml.WriteAttributeString(name, longLongValue.ToString(culture));
                                logger.LogTrace($"Wrote attribute long long {name}={longLongValue}.");
                                break;

                            case (int)FieldTypes.SizeT:
                                id = inBinary.ReadUInt16();
                                var ulongValue = inBinary.ReadUInt32();
                                name = GetName(currentIds, id);
                                contentXml.WriteAttributeString(name, ulongValue.ToString(culture));
                                logger.LogTrace($"Wrote attribute sizet {name}={ulongValue}.");
                                break;

                            case (int)FieldTypes.Data:
                                count = inBinary.ReadInt32();
                                text = ReadString(inBinary, count, encoding);
                                contentXml.WriteString(text);
                                logger.LogTrace($"Wrote text '{text}'.");
                                break;

                            case (int)FieldTypes.Raw:
                                count = inBinary.ReadInt32();
                                text = ReadString(inBinary, count, encoding);
                                contentXml.WriteRaw(text);
                                logger.LogTrace($"Wrote raw '{text}'.");
                                break;

                            case (int)FieldTypes.CharSize:
                                int readByte = inStream.ReadByte();
                                encoding = readByte switch
                                {
                                    1 => Encoding.UTF8,
                                    2 => Encoding.Unicode,
                                    4 => Encoding.UTF32,
                                    _ => throw new InvalidOperationException(
                                        $"Invalid encoding identifier '{readByte}'.")
                                };

                                logger.LogTrace($"Set encoding '{encoding}'.");
                                break;

                            default:
                                var msg = $"Unknown field type id {currentValue}.";
                                logger.LogError(msg);
                                throw new InvalidOperationException(msg);
                        }

                        // read the value indicating the next type of data
                        currentValue = inBinary.Read();
                    }

                    // ensure all xml content has been written
                    contentXml.Flush();

                    // get the xml as a string
                    var content = contentWriter.ToString();

                    // write the content to a stream writer that can be used by the audacity serializer
                    using var outStream = new MemoryStream();
                    using var outWriter = new StreamWriter(outStream);
                    outWriter.Write(content);
                    outWriter.Flush();
                    outStream.Position = 0;
                    project = this.audacitySerializer.Deserialize(outStream, fileInfo.FullName);
                }
            });
            return project;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Based on https://github.com/audacity/audacity/blob/2c47dc5ba1a91c87a457ffba6caf8b1baf706279/src/ProjectSerializer.cpp
        /// </remarks>
        private enum FieldTypes
        {
            CharSize,
            StartTag,
            EndTag,
            String,
            Int,
            Bool,
            Long,
            LongLong,
            SizeT,
            Float,
            Double,
            Data,
            Raw,
            Push,
            Pop,
            Name,
        };

        private string ReadString(BinaryReader reader, int count, Encoding encoding)
        {
            var bytes = new byte[count];
            var readCount = reader.Read(bytes, 0, count);
            var text = encoding.GetString(bytes);
            return text;
        }

        private string GetName(Dictionary<ushort, string> dict, ushort id)
        {
            var value = dict[id];
            return value;
        }
    }
}