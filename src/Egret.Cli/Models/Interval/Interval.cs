
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
using static Egret.Cli.Models.IntersectionDetails;
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

        public Interval(double minimum, double maximum, Topology topology) : this()
        {
            Minimum = minimum;
            Maximum = maximum;
            Topology = topology;
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
        public static Interval Empty(double value)
        {
            return new Interval()
            {
                Maximum = value,
                Minimum = value,
                Middle = value,
                // anything other than Inclusive is Empty (as per IsEmpty below)
                Topology = Exclusive,
            };
        }

        public static Interval FromTolerance(double value, double tolerance)
        {
            return new Interval()
            {
                Minimum = value - tolerance,
                Maximum = value + tolerance,
                Middle = value,
                Topology = Inclusive
            };
        }

        public static Interval Approximation(double value)
        {
            var fivePercent = value * 0.05;
            return new Interval()
            {
                Minimum = value - fivePercent,
                Maximum = value + fivePercent,
                Middle = value,
                Topology = Inclusive
            };
        }

        public static Interval SameOrderOfMagnitudeAs(double value)
        {
            return new Interval()
            {
                Minimum = Math.Pow(value, 0.1),
                Maximum = Math.Pow(value, 10),
                Middle = value,
                Topology = Inclusive
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

        private static readonly byte[] approximateSymbol = Encoding.UTF8.GetBytes("≈");
        private static readonly byte[] greaterEqualSymbol = Encoding.UTF8.GetBytes("≥");
        private static readonly byte[] lessEqualSymbol = Encoding.UTF8.GetBytes("≤");
        private static readonly byte[] toleranceSymbol = Encoding.UTF8.GetBytes("±");
        private static readonly byte[] epsilonSymbol = Encoding.UTF8.GetBytes("ε");

        public static Interval FromString(ReadOnlySpan<byte> toParse, double defaultTolerance)
        {
            // see docs/interval.ebnf for valid formats we can parse here
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
                if (!next.StartsWith(toleranceSymbol))
                {
                    error = "'±' not found after number";
                }

                consumed += toleranceSymbol.Length;
                next = next[toleranceSymbol.Length..];


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
            else if (ParseApproximation(toParse, out var approximation, ref error))
            {
                return approximation;
            }
            else if (ParseRelational(toParse, out var relational, ref error))
            {
                // relational
                return relational;
            }
            else
            {
                error ??= "Unknown format";
            }

            throw new ArgumentException($"Failed to parse `{toParse.ToString()}` as an interval. {error}");

            bool ParseNumber(ReadOnlySpan<byte> span, out double value, out int consumed)
            {
                if (span.StartsWith(epsilonSymbol))
                {
                    consumed = epsilonSymbol.Length;
                    value = defaultTolerance;
                    return true;
                }

                return Utf8Parser.TryParse(span, out value, out consumed);
            }

            bool ParseApproximation(ReadOnlySpan<byte> span, out Interval value, ref string error)
            {
                ReadOnlySpan<byte> next;
                Func<double, Interval> factory;

                if (span[0] is (byte)'~')
                {

                    next = span[1..];
                    factory = SameOrderOfMagnitudeAs;

                }
                else if (span.StartsWith(approximateSymbol))
                {

                    next = span[approximateSymbol.Length..];
                    factory = Approximation;
                }
                else
                {
                    value = default;
                    error = null;
                    return false;
                }


                if (!ParseNumber(next, out var number, out var consumed))
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

                value = factory.Invoke(number);
                return true;
            }

            bool ParseRelational(ReadOnlySpan<byte> span, out Interval value, ref string error)
            {
                Topology topology;
                ReadOnlySpan<byte> next;
                bool lowAnchor;
                if (span[0] is (byte)'>')
                {
                    topology = MinimumExclusiveMaximumInclusive;
                    next = span[1..];
                    lowAnchor = true;

                }
                else if (span[0] is (byte)'<')
                {
                    topology = MinimumExclusiveMaximumInclusive;
                    next = span[1..];
                    lowAnchor = false;
                }
                else if (span.StartsWith(lessEqualSymbol))
                {
                    topology = Inclusive;
                    next = span[lessEqualSymbol.Length..];
                    lowAnchor = false;
                }
                else if (span.StartsWith(greaterEqualSymbol))
                {
                    topology = Inclusive;
                    next = span[greaterEqualSymbol.Length..];
                    lowAnchor = true;
                }
                else
                {
                    value = default;
                    error = null;
                    return false;
                }

                if (!ParseNumber(next, out var anchor, out var consumed))
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
                    Middle = anchor,
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
                    error = null;
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
            return ToString(null);
        }

        public string ToString(string endPointFormat)
        {
            var left = IsMinimumInclusive ? '[' : '(';
            var right = IsMaximumInclusive ? ']' : ')';
            return $"{left}{this.Minimum.ToString(endPointFormat)}, {this.Maximum.ToString(endPointFormat)}{right}";
        }

        public string ToString(bool simplify, string endPointFormat = null)
        {
            return this switch
            {
                _ when !simplify => ToString(endPointFormat),
                { IsDegenerate: true } => Center.ToString(endPointFormat),
                _ => ToString(endPointFormat)
            };
        }


        public double Range => Maximum - Minimum;

        public double Radius => Math.Abs(Minimum - Maximum) / 2;

        public bool IsMinimumInclusive => Topology.IsMinimumInclusive();

        public bool IsMaximumInclusive => Topology.IsMaximumInclusive();

        public bool IsDegenerate => Minimum == Maximum && Topology == Inclusive;
        public bool IsEmpty => Minimum == Maximum && Topology != Inclusive;

        public bool IsProper => !IsDegenerate && !IsEmpty;

        public bool IsLeftBounded => Minimum != double.NegativeInfinity;
        public bool IsRightBounded => Maximum != double.NegativeInfinity;
        public bool IsBounded => IsLeftBounded && IsRightBounded;

        public (Interval Lower, Interval Point, Interval Upper) Partition(double point)
        {
            return (
                    new Interval(Math.Min(Minimum, point), point, Topology.OpenMaximum()),
                    Empty(point),
                    new Interval(point, Math.Max(Maximum, point), Topology.OpenMinimum())
                );
        }


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


        public bool IntersectsWith(Interval other)
        {
            var a1 = Minimum;
            var a2 = Maximum;
            var b1 = other.Minimum;
            var b2 = other.Maximum;

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
                var a when a2 == b1 => a.Topology.IsCompatibleWith(other.Topology),
                // BA           touching, possibly overlapping
                var a when a1 == b2 => other.Topology.IsCompatibleWith(a.Topology),
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

        // public IntersectionDetails GetIntersection(Interval other)
        // {
        //     var a1 = Minimum;
        //     var a2 = Maximum;
        //     var b1 = other.Minimum;
        //     var b2 = other.Maximum;

        //     // there are 9 possible placements we care about
        //     return this switch
        //     {
        //         // A  B         non-overlapping
        //         _ when a2 < b1 => FullyBelow,
        //         // B  A         non-overlapping
        //         _ when b2 < a1 => FullyAbove,
        //         // B==A         B and A are exactly equal
        //         _ when a1 == b1 && a2 == b2 => Intersecting,
        //         // AB           touching, possibly overlapping
        //         var a when a2 == b1 => a.Topology.IsCompatibleWith(other.Topology),
        //         // BA           touching, possibly overlapping
        //         var a when a1 == b2 => other.Topology.IsCompatibleWith(a.Topology),
        //         // B--A--B      B wholly contains A
        //         _ when b1 < a1 && a2 < b1 => true,
        //         // A--B--A      A wholly contains B
        //         _ when a1 < b1 && b2 < a2 => true,
        //         // A--B==A--B   A and B overlap
        //         _ when b1 < a2 => true,
        //         // B--A==B--A   B and A overlap
        //         _ when a1 < b2 => true,
        //         _ => throw new InvalidOperationException()
        //     };
        // }

        // public Interval Union(Interval other)
        // {

        // }

    }

    [Flags]
    public enum IntersectionDetails : byte
    {
        /// <summary>
        /// A does not overlap with B
        /// </summary>
        Disjoint = 0,

        /// <summary>
        /// A does overlaps with B
        /// </summary>
        Intersecting = 1,

        /// <summary>
        /// A contains B
        /// </summary>
        Contains = 2,

        /// <summary>
        /// A is contained by B
        /// </summary>
        ContainedBy = 4,

        /// <summary>
        /// If either <see cref="Superset" /> or <see cref="Subset" /> is set
        /// then proper indicates that the endpoints of A and B do not touch.
        /// </summary>
        Proper = 8,

        /// <summary>
        /// If A's minimum endpoint is the same as B's maximum endpoint.
        /// If A and B have compatible topologies then they <see cref="Intersect" /> as well.
        /// </summary>
        MinEqualsMax = 16,
        /// <summary>
        /// If A's maximum endpoint is the same as B's minimum endpoint.
        /// If A and B have compatible topologies then they <see cref="Intersect" /> as well.
        /// </summary>
        MaxEqualsMin = 32,

        MinBelow = 64,
        MaxBelow = 128,

        FullyBelow = Disjoint | MinBelow | MaxBelow,
        FullyAbove = Disjoint,
        ProperSubset = Intersecting | ContainedBy | Proper,
        Subset = Intersecting | ContainedBy,

        ProperSuperset = Intersecting | Contains | Proper,
        Superset = Intersecting | Contains,
    }
}