using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    /// <summary>
    /// Represents a temporal band of audio.
    // Coordinates are encoded as a pair of intervals to allow fuzzy matching.
    /// <summary>
    public struct TimeRange : IYamlConvertible
    {
        public TimeRange(Interval startSeconds, Interval endSeconds) : this()
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
        }


        public Interval StartSeconds { get; private set; }

        public Interval EndSeconds { get; private set; }

        public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
        {
            parser.Consume<SequenceStart>();

            // the interval type converter consumes one scalar token per invocation
            this.StartSeconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            this.EndSeconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            parser.Consume<SequenceEnd>();
        }

        public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
        {
            nestedObjectSerializer.Invoke(new[] {
                StartSeconds,
                EndSeconds,
            }, typeof(Interval[]));
        }
    }




}