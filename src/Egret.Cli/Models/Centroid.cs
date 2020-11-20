using System;
using System.Numerics;
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
        public Interval StartSeconds { get; private set; }

        public Interval LowHertz { get; private set; }

        public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
        {
            parser.Consume<SequenceStart>();

            // the interval type converter consumes one scalar token per invocation
            this.StartSeconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            this.LowHertz = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            parser.Consume<SequenceEnd>();
        }

        public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
        {
            nestedObjectSerializer.Invoke(new[] {
                this.StartSeconds,
                this.LowHertz,
            }, typeof(Interval[]));
        }

        public Vector2 ToCoordinates()
         => new Vector2((float)StartSeconds.Middle, (float)LowHertz.Middle);
    }




}