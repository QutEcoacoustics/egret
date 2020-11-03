
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Unicode;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    public class Config
    {
        public Dictionary<string, Tool> Tools { get; set; }

        public Dictionary<string, Suite> TestSuites { get; set; }

        public Dictionary<string, Case[]> CommonCases { get; set; }
    }

    public class Tool
    {
        public string Command { get; set; }

        public string ResultPattern { get; set; }
    }

    public class Suite
    {

        public string Name { get; set; }

        public string DefaultLabel { get; set; }

        public Case[] Cases { get; set; }

        public string[] IncludeCases { get; set; } = Array.Empty<string>();

        public Dictionary<string, Case[]> SharedCases { get; set; } = new();

        public Dictionary<string, object> ToolConfigs { get; set; }

    }

    public class Case
    {
        public Expectation[] ExpectEvents { get; set; }
        public AggregateExpectation[] Expect { get; set; }

        public string File { get; set; }

        public Uri Uri { get; set; }
    }



    public class Expectation
    {
        public string Label { get; set; }

        public string[] Labels { get; set; }

        public Bounds Bounds { get; set; }

        public Centroid Centroid { get; set; }

        public bool Match { get; set; } = true;


        public Interval Duration { get; set; }
        public Interval Bandwidth { get; set; }

        public string Condition { get; set; }

    }

    public abstract class AggregateExpectation
    {

    }

    public class NoEvents : AggregateExpectation
    {

    }

    public class EventCount : AggregateExpectation
    {
        public int Count { get; set; }
    }


    public enum Topology : byte
    {
        /*
         * Our flags are defined like this to ensure the default value is [a,b).
         * The meaning of bit 1 is: Is the left exclusive? (1 yes, 0 no)
         * The meaning of bit 2 is: Is the right inclusive? (1 yes, 0 no)
         *
         * Note bit 1 is on the right.
         */

        Open = 0b0_1,
        LeftClosedRightOpen = 0b0_0,
        LeftOpenRightClosed = 0b1_1,
        Closed = 0b1_0,


        Exclusive = Open,
        MinimumInclusiveMaximumExclusive = LeftClosedRightOpen,
        MinimumExclusiveMaximumInclusive = LeftOpenRightClosed,
        Inclusive = Closed,

        Default = LeftClosedRightOpen,
    }

    /// <summary>
    /// Represents a geometric center of an event.
    // Coordinates are encoded as a pair of intervals to allow fuzzy matching.
    /// <summary>
    public struct Centroid : IYamlConvertible
    {
        public Interval Seconds { get; set; }

        public Interval Hertz { get; set; }

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
            throw new NotImplementedException();
        }
    }
    /// <summary>
    /// Represents a rectangular event.
    // Coordinates are encoded as four intervals to allow fuzzy matching.
    /// <summary>
    public struct Bounds : IYamlConvertible
    {
        public Interval StartSeconds { get; set; }

        public Interval EndSeconds { get; set; }

        public Interval LowHertz { get; set; }

        public Interval HighHertz { get; set; }

        public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
        {
            parser.Consume<SequenceStart>();

            // the interval type converter consumes one scalar token per invocation
            this.StartSeconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));


            this.EndSeconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));


            this.LowHertz = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            this.HighHertz = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

            parser.Consume<SequenceEnd>();

        }

        public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
        {
            throw new NotImplementedException();
        }
    }




}