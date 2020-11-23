
using LanguageExt.ClassInstances;
using System;
using System.Buffers.Text;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using Egret.Cli.Maths;
using LanguageExt;
using static Egret.Cli.Models.Topology;
using System.Collections.Generic;
using MoreLinq;

namespace Egret.Cli.Models
{
    public readonly struct Interval
    {
        public static readonly Interval Unit = new Interval(0, 1);

        public Interval(double minimum, double maximum) : this()
        {
            Minimum = minimum;
            Maximum = maximum;
            Middle = (minimum, maximum).Center();
        }

        public double Minimum { get; init; }

        public double Maximum { get; init; }
        public Topology Topology { get; init; }

        /// <summary>
        /// Gets the original value of a interval that specified as a target value and tolerance tuple.
        /// In cases where a interval was defined as minimum and maximum it instead gets the 
        /// geometric mean of the interval. 
        /// In the case of a relation (or an interval with an infinite bound) it returns whichever bound is not infinite.
        /// </summary>
        /// <remarks>
        /// This property is designed to be give a useful real number that allows the interval to placed in a domain.
        /// If you need the strict mathematical definition of the midpoint then see <c>Center</c>.
        /// <value></value>
        public double Middle { get; init; }

        /// <summary>
        /// Gets the midpoint of the interval.
        /// </summary>
        /// <remarks>
        /// Differs from <c>Middle</c> in that is the mathematical definition only.
        /// If either endpoint is Infinite, the result will be infinite.
        /// </remarks>
        /// <returns>The midpoint.</returns>
        public double Center => (Minimum, Maximum).Center();


        public static Interval Degenerate(double value)
        {
            return new Interval()
            {
                Maximum = value,
                Minimum = value,
                Middle = value,
                // Topology must be inclusive otherwise it represents an empty set.
                // i.e. with default topology [,) the result is x <= x < x - which is always false, i.e. an empty set
                Topology = Inclusive,
            };
        }

        public static Interval FromTolerance(double value, double tolerance)
        {
            return new Interval()
            {
                Maximum = value - tolerance,
                Minimum = value + tolerance,
                Middle = value
            };
        }

        /// <summary>
        /// The closure of I is the smallest closed interval that contains I; which is also the set I augmented with its finite endpoints.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Interval ClosureOf(IEnumerable<double> points)
        {
            var (min, max) = points.Aggregate(
                double.PositiveInfinity, (extrema, value) => Math.Min(extrema, value),
                double.NegativeInfinity, (extrema, value) => Math.Max(extrema, value),
                (min, max) => (min, max)
            );

            return new Interval()
            {
                Maximum = max,
                Minimum = min,
                Middle = (max, min).Center(),
                Topology = Open,
            };
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
                    ('[', ']') => Closed,
                    ('[', ')') => LeftClosedRightOpen,
                    ('(', ']') => LeftOpenRightClosed,
                    ('(', ')') => Open,
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

        public override string ToString()
        {
            var left = IsMinimumInclusive ? '[' : '(';
            var right = IsMaximumInclusive ? ']' : ')';
            return $"{left}{this.Minimum}, {this.Maximum}{right}";
        }

        public string ToString(bool simplify)
        {
            return this switch
            {
                _ when !simplify => ToString(),
                { IsDegenerate: true } => Center.ToString(),
                _ => ToString()
            };
        }


        public double Range => Maximum - Minimum;

        public bool IsMinimumInclusive => Topology.IsMinimumInclusive();

        public bool IsMaximumInclusive => Topology.IsMaximumInclusive();

        public bool IsDegenerate => Minimum == Maximum && Topology == Inclusive;
        public bool IsEmpty => Minimum == Maximum && Topology != Inclusive;

        /// <summary>
        /// Gets the largest open interval (excluding endpoints) within the bounds of the current interval.
        /// </summary>
        /// <returns>A new interior interval</returns>
        public Interval Interior => new Interval() { Minimum = Minimum, Maximum = Maximum, Middle = Middle, Topology = Exclusive };

        public double UnitNormalize(double value, bool clamp = true)
        {
            var v = (value - Minimum) / Range;

            return clamp ? Math.Clamp(v, 0, 1) : v;
        }

        public bool Contains(double value)
        {
            return Topology switch
            {
                Exclusive => Minimum < value && value < Maximum,
                MinimumInclusiveMaximumExclusive => Minimum <= value && value < Maximum,
                MinimumExclusiveMaximumInclusive => Minimum < value && value <= Maximum,
                Inclusive => Minimum <= value && value <= Maximum,
                _ => throw new ArgumentException($"Invalid topology encountered: {Topology}")
            };
        }


        public bool IntersectsWith(Interval b)
        {
            var a1 = Minimum;
            var a2 = Maximum;
            var b1 = b.Minimum;
            var b2 = b.Maximum;

            // there are 9 possible placements we care about
            return this switch
            {
                // A  B         non-overlapping
                _ when a2 < b1 => false,
                // B  A         non-overlapping
                _ when b2 < a1 => false,
                // B==A         B and A are exactly equal
                _ when a1 == b1 && a2 == b2 => true,
                // AB           touching, possibly overlapping
                var a when a2 == b1 => a.Topology.IsCompatibleWith(b.Topology),
                // BA           touching, possibly overlapping
                var a when a1 == b2 => b.Topology.IsCompatibleWith(a.Topology),
                // B--A--B      B wholly contains A
                _ when b1 < a1 && a2 < b1 => true,
                // A--B--A      A wholly contains B
                _ when a1 < b1 && b2 < a2 => true,
                // A--B==A--B   A and B overlap
                _ when b1 < a2 => true,
                // B--A==B--A   B and A overlap
                _ when a1 < b2 => true,
                _ => throw new InvalidOperationException()
            };
        }
    }

    public static class IntervalExtensions
    {
        public static bool IntersectsWith(this double value, Interval interval) => interval.Contains(value);
    }
}