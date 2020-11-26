
using Egret.Cli.Processing;
using LanguageExt;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using static LanguageExt.Prelude;

namespace Egret.Cli.Models
{
    public interface ITryGetValue
    {
        bool TryGetValue<T>(string key, out T value, StringComparison comparison = StringComparison.InvariantCulture);
    }

    public record KeyedValue<T>(string Key, T Value);

    public record CompositeKeyedValue<T> : KeyedValue<T>
    {
        public const string CompositeKeyDelimiter = "+";

        public CompositeKeyedValue(IEnumerable<string> keys, T Value)
            : base(keys.Join(CompositeKeyDelimiter), Value)
        {
            Keys = keys;
        }

        public IEnumerable<string> Keys { get; init; }
    }


    public abstract class NormalizedResult : ITryGetValue
    {


        public abstract bool TryGetValue<T>(string key, out T value, StringComparison comparison = StringComparison.InvariantCulture);

        private Validation<string, KeyedValue<string>>? label;
        private Validation<string, KeyedValue<double>>? start;
        private Validation<string, KeyedValue<double>>? centroidStart;
        private Validation<string, KeyedValue<double>>? end;
        private Validation<string, KeyedValue<double>>? low;
        private Validation<string, KeyedValue<double>>? centroidLow;
        private Validation<string, KeyedValue<double>>? high;
        private Validation<string, KeyedValue<double>>? bandwidth;
        private Validation<string, KeyedValue<double>>? duration;

        public Validation<string, KeyedValue<string>> Label => label ??= Munging.TryNames<string>(this, Munging.LabelNames);
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
    }

    public class JsonResult : NormalizedResult
    {
        private readonly JObject source;

        public JsonResult(JObject source)
        {
            this.source = source;
        }

        public override bool TryGetValue<T>(string key, out T value, StringComparison comparison = StringComparison.InvariantCulture)
        {

            var propExists = source.TryGetValue(key, comparison, out var valueElement);
            if (propExists)
            {
                value = valueElement.ToObject<T>();
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}