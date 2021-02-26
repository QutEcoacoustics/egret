
using Egret.Cli.Extensions;
using Egret.Cli.Processing;
using Egret.Cli.Serialization.Yaml;
using LanguageExt;
using LanguageExt.ClassInstances;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using YamlDotNet.Core.Tokens;
using static LanguageExt.Prelude;

namespace Egret.Cli.Models
{



    public abstract class NormalizedResult : ITryGetValue, ISourceInfo
    {

        public bool IsMarked => MarkedBy is not null;
        public IExpectation MarkedBy { get; private set; }

        public void Mark(IExpectation expectation)
        {
            if (IsMarked)
            {
                throw new InvalidOperationException("Result reserved twice. This should not happen");
            }
            MarkedBy = expectation;
        }

        public abstract SourceInfo SourceInfo { get; set; }

        public abstract bool TryGetValue<T>(string key, out T value, StringComparison comparison = StringComparison.InvariantCulture);

        private Validation<string, KeyedValue<IEnumerable<string>>>? labels;
        private Validation<string, KeyedValue<double>>? start;
        private Validation<string, KeyedValue<double>>? centroidStart;
        private Validation<string, KeyedValue<double>>? end;
        private Validation<string, KeyedValue<double>>? low;
        private Validation<string, KeyedValue<double>>? centroidLow;
        private Validation<string, KeyedValue<double>>? high;
        private Validation<string, KeyedValue<double>>? bandwidth;
        private Validation<string, KeyedValue<double>>? duration;

        public Validation<string, KeyedValue<IEnumerable<string>>> Labels
        {
            get
            {
                return labels ??= Evaluate();

                Validation<string, KeyedValue<IEnumerable<string>>> Evaluate()
                {
                    var a = Munging.TryNames<string>(this, Munging.LabelNames)
                            .Map(kv => new KeyedValue<IEnumerable<string>>(kv.Key, kv.Value.One()));
                    var b = Munging.TryNames<IEnumerable<string>>(this, Munging.LabelsNames);
                    return a.Or(b);
                }
            }
        }

        public Validation<string, KeyedValue<double>> Start => start ??= Munging.TryNames<double>(this, Munging.StartNames);
        public Validation<string, KeyedValue<double>> End => end ??= Munging.TryNames<double>(this, Munging.EndNames);
        public Validation<string, KeyedValue<double>> Low => low ??= Munging.TryNames<double>(this, Munging.LowNames);
        public Validation<string, KeyedValue<double>> High => high ??= Munging.TryNames<double>(this, Munging.HighNames);
        public Validation<string, KeyedValue<double>> CentroidTime => centroidStart ??= Munging.TryNames<double>(this, Munging.CentroidStartNames);
        public Validation<string, KeyedValue<double>> CentroidFrequency => centroidLow ??= Munging.TryNames<double>(this, Munging.CentroidLowNames);
        public Validation<string, KeyedValue<double>> Bandwidth
        {
            get
            {
                return bandwidth ??= Munging.TryNames<double>(this, Munging.BandWidthNames) || CompositeBandwidth();
            }
        }

        public Validation<string, KeyedValue<double>> CompositeBandwidth()
        {
            var composite =
                from h in High
                from l in Low
                select (KeyedValue<double>)new CompositeKeyedValue<double>(Seq(h.Key, l.Key), h.Value - l.Value);
            return composite;
        }

        public Validation<string, KeyedValue<double>> Duration
        {
            get
            {
                return duration ??= Munging.TryNames<double>(this, Munging.DurationNames) || CompositeDuration();
            }
        }

        public Validation<string, KeyedValue<double>> CompositeDuration()
        {
            var composite =
                from e in End
                from s in Start
                select (KeyedValue<double>)new CompositeKeyedValue<double>(Seq(e.Key, s.Key), e.Value - s.Value);
            return composite;
        }

        public Validation<string, (Vector2 BotLeft, Vector2 TopRight)> Bounds
        {
            get
            {
                var coords = (Start, Low, End, High)
                    // should collect all values or all errors
                    .Apply(
                        (s, l, e, h) => (
                            new Vector2((float)s.Value, (float)l.Value),
                            new Vector2((float)e.Value, (float)h.Value)
                        )
                    )
                    .Match<Validation<string, (Vector2 BotLeft, Vector2 TopRight)>>(
                        Succ: value => value,
                        Fail: errors => "Could not form bounding box from given result data".Cons(errors)
                    );
                return coords;
            }
        }

        public Validation<string, Vector2> Centroid
        {
            get
            {
                var coords = (CentroidTime, CentroidFrequency)
                    // should collect all values or all errors
                    .Apply(
                        (s, l) => new Vector2((float)s.Value, (float)l.Value)
                    )
                    .Match<Validation<string, Vector2>>(
                        Succ: value => value,
                        Fail: errors => "Could not form centroid from given result data".Cons(errors)
                    );
                return coords;
            }
        }

        public Validation<string, (double Start, double End)> Time
        {
            get
            {
                var coords = (Start, End)
                    // should collect all values or all errors
                    .Apply(
                        (s, e) => (s.Value, e.Value)
                    )
                    .Match<Validation<string, (double Start, double End)>>(
                        Succ: value => value,
                        Fail: errors => "Could not form time range from given result data".Cons(errors)
                    );
                return coords;
            }
        }

        public override string ToString()
        {

            return $"{SourceInfo.ToString(true)}: Label[s]={Format(Labels)} Start={Format(Start)} End={Format(End)} Low={Format(Low)} High={Format(High)}";

            static string Format<T>(Validation<string, KeyedValue<T>> validation)
            {
                return validation.Match(
                    Succ: (s) => $"{{{s.Key}: {FormatValue(s.Value)}}}",
                    Fail: (errors) => "ERROR:" + errors.Join(";", "\"")
                );
            }

            static string FormatValue<T>(T value) => value is IEnumerable<string> list ? list.JoinIntoSetNotation() : value.ToString();
        }

    }
}