using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    /// <summary>
    /// Represents a rectangular event.
    // Coordinates are encoded as four intervals to allow fuzzy matching.
    /// <summary>
    public struct Bounds : IYamlConvertible
    {
        public Interval StartSeconds { get; private set; }

        public Interval EndSeconds { get; private set; }

        public Interval LowHertz { get; private set; }

        public Interval HighHertz { get; private set; }

        public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
        {
            parser.Consume<SequenceStart>();

            // the interval type converter consumes one scalar token per invocation
            this.StartSeconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            this.LowHertz = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            this.EndSeconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            this.HighHertz = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            parser.Consume<SequenceEnd>();

        }

        public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
        {
            nestedObjectSerializer.Invoke(new[] {
                this.StartSeconds,
                this.LowHertz,
                this.EndSeconds,
                this.HighHertz
            }, typeof(Interval[]));
        }
    }




}