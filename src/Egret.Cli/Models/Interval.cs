
using System;
using System.Buffers.Text;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    public record Interval : IYamlConvertible
    {
        private const string tolerance = "±";

        public double Minimum { get; set; }

        public double Maximum { get; set; }
        public Topology Topology { get; set; }

        /// <summary>
        /// Gets the original value of a interval value specified as a target and tolerance tuple.
        /// In cases where a interval was defined as minimum and maximum it instead gets the 
        /// geometric mean of the interval. 
        /// </summary>
        /// <value></value>
        public double Center { get; set; }

        public void Read(IParser parser, Type expectedType, ObjectDeserializer nestedObjectDeserializer)
        {
            var value = parser.Consume<Scalar>();
        }

        public void Write(IEmitter emitter, ObjectSerializer nestedObjectSerializer)
        {
            throw new NotImplementedException();
        }

        public static Interval Degenerate(double value)
        {
            return new Interval() { Maximum = value, Minimum = value };
        }

        public static Interval FromTolerance(double value, double tolerance)
        {
            return new Interval() { Maximum = value - tolerance, Minimum = value + tolerance };
        }

        public static Interval FromString(ReadOnlySpan<byte> toParse, double defaultTolerance)
        {
            // 1±0.2
            // '>1'
            // '<1'
            // '≥1'
            // '≤1'
            // 1±ε
            // '[1, 2]'
            // '(1, 2]'
            // '[1, 2)'
            // '(1, 2)'
            string error = null;
            if (ParseNumber(toParse, out var number, out var consumed))
            {
                // number or tolerance
                if (consumed == toParse.Length)
                {
                    // number
                    return Degenerate(number);
                }

                // tolerance
                var next = toParse[consumed..];
                if (next[0] is not (byte)'±')
                {
                    error = "'±' not found after number";
                }


                if (!ParseNumber(next, out var tolerance, out var consumedNext))
                {
                    error = $" `{next.ToString()}` is not a valid number";
                }

                if (consumed != next.Length)
                {
                    error = $"Characters left over: {next[consumedNext..].ToString()}";
                }

                return FromTolerance(number, tolerance);

            }
            else if (toParse[0] is (byte)'[' or (byte)'(' && ParseBounded(toParse, out var bounded, ref error))
            {
                // bounded
                return bounded;

            }
            else if (ParseRelational(toParse, out var relational, ref error))
            {
                // relational
                return relational;
            }
            else
            {
                error = "Unknown format";
            }

            throw new ArgumentException($"Failed to parse `{toParse.ToString()}` as an interval. {error}");

            bool ParseNumber(ReadOnlySpan<byte> span, out double value, out int consumed)
            {
                if (span.StartsWith(Encoding.UTF8.GetBytes("ε")))
                {
                    consumed = 1;
                    value = defaultTolerance;
                    return true;
                }

                return Utf8Parser.TryParse(span, out value, out consumed);
            }

            bool ParseRelational(ReadOnlySpan<byte> span, out Interval value, ref string error)
            {
                Topology topology;
                ReadOnlySpan<byte> next;
                bool lowAnchor;
                if (span[0] is (byte)'>')
                {
                    topology = Topology.MinimumExclusiveMaximumInclusive;
                    next = span[1..];
                    lowAnchor = true;

                }
                else if (span[0] is (byte)'<')
                {
                    topology = Topology.MinimumExclusiveMaximumInclusive;
                    next = span[1..];
                    lowAnchor = false;
                }
                else if (span.StartsWith(Encoding.UTF8.GetBytes("≤")))
                {
                    topology = Topology.Inclusive;
                    next = span[2..];
                    lowAnchor = false;
                }
                else if (span.StartsWith(Encoding.UTF8.GetBytes("≥")))
                {
                    topology = Topology.Inclusive;
                    next = span[2..];
                    lowAnchor = true;
                }
                else
                {
                    value = default;
                    error = "Unknown relational operator. Must be one of >, <, ≥, ≤";
                    return false;
                }

                if (!ParseNumber(span[1..], out var anchor, out var consumed))
                {
                    value = default;
                    error = $" `{span.ToString()}` is not a valid number";
                    return false;
                };

                if (consumed != next.Length)
                {
                    value = default;
                    error = $"Characters left over: {next.ToString()}";
                    return false;
                }

                value = new Interval()
                {
                    Minimum = lowAnchor ? anchor : double.NegativeInfinity,
                    Maximum = lowAnchor ? double.PositiveInfinity : anchor,
                    Topology = topology
                };
                return true;
            }

            bool ParseBounded(ReadOnlySpan<byte> span, out Interval value, ref string error)
            {
                value = default;
                Topology? topology = ((char)span[0], (char)span[^1]) switch
                {
                    ('[', ']') => Topology.Closed,
                    ('[', ')') => Topology.LeftClosedRightOpen,
                    ('(', ']') => Topology.LeftOpenRightClosed,
                    ('(', ')') => Topology.Open,
                    _ => null
                };

                if (!topology.HasValue)
                {
                    error = "Unknown interval notation";
                    return false;
                }

                if (!ParseNumber(span[1..], out var minimum, out var minConsumed))
                {
                    error = $"Count not parse interval minimum in {span.ToString()}";
                    return false;
                }

                if (span[minConsumed] != (byte)',')
                {
                    error = $"Missing `,` (the comma) in the interval {span.ToString()}";
                }


                if (!ParseNumber(span[(minConsumed + 1)..], out var maximum, out var maxConsumed))
                {
                    error = $"Count not parse interval maximum in {span.ToString()}";
                    return false;
                }

                if (maxConsumed != span.Length - 1)
                {
                    error = "extra characters at end of interval";
                }

                value = new Interval()
                {
                    Minimum = minimum,
                    Maximum = maximum,
                    Topology = topology.Value
                };
                return true;
            }

        }

    }




}