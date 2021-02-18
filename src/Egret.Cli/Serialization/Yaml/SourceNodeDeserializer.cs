using Egret.Cli.Models;
using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Egret.Cli.Serialization.Yaml
{
    public class SourceInfoNodeDeserializer : INodeDeserializer
    {
        private readonly INodeDeserializer original;
        public string CurrentSource { get; }
        public SourceInfoNodeDeserializer(INodeDeserializer original, string currentSource)
        {
            CurrentSource = currentSource;
            this.original = original;
        }

        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            var start = reader.Current;
            var originalResult = original.Deserialize(reader, expectedType, nestedObjectDeserializer, out value);

            if (originalResult && value is ISourceInfo info)
            {
                var end = reader.Current;
                info.SourceInfo = new SourceInfo(CurrentSource, start.Start.Line, start.Start.Column, end.Start.Line, end.Start.Column);
            }

            return originalResult;
        }
    }
}