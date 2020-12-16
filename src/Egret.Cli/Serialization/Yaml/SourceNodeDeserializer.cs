using Egret.Cli.Models;
using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Egret.Cli.Serialization.Yaml
{
    public class SourceInfoNodeDeserializer : INodeDeserializer
    {
        private readonly INodeDeserializer original;
        public SourceInfoNodeDeserializer(INodeDeserializer original)
        {
            this.original = original;
        }

        public string CurrentSource { get; internal set; }

        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            var start = reader.Current;
            var originalResult = original.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);

            if (originalResult && value is ISourceInfo info)
            {
                var end = reader.Current;
                info.SourceInfo = new SourceInfo(CurrentSource, start.Start.Line, start.Start.Index, end.Start.Line, end.Start.Index);
            }

            return originalResult;
        }
    }
}