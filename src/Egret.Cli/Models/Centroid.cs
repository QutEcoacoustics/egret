using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    /// <summary>
    /// Represents a geometric center of an event.
    // Coordinates are encoded as a pair of intervals to allow fuzzy matching.
    /// <summary>
    public struct Centroid : IYamlConvertible
    {
        public Interval Seconds { get; private set; }

        public Interval Hertz { get; private set; }

        public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
        {
            parser.Consume<SequenceStart>();

            // the interval type converter consumes one scalar token per invocation
            this.Seconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            this.Hertz = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            parser.Consume<SequenceEnd>();
        }

        public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
        {
            nestedObjectSerializer.Invoke(new[] {
                this.Seconds,
                this.Hertz,
            }, typeof(Interval[]));
        }
    }




}