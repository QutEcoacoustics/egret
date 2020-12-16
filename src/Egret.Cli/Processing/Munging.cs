
using Egret.Cli.Models;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core.Tokens;
using static LanguageExt.Prelude;

namespace Egret.Cli.Processing
{
    public class Munging
    {

        public static readonly IEnumerable<string> LabelNames = new string[]
        {
            "label",
            "tag",
            "species",
            "name",
            "species name"
        };

        public static readonly IEnumerable<string> LabelsNames = new string[]
        {
            "labels",
            "tags",
            "names"
        };

        public static readonly IEnumerable<string> StartNames = new string[]
        {
            "start",
            "event start seconds",
            "start offset seconds",
            "start seconds",
            "event start offset seconds",
            "start offset",
        };

        public static readonly IEnumerable<string> CentroidStartNames = new string[]
        {
            "time",
            "centroid time seconds",
            "centroid time",
            "seconds",
            "start",
            "event start seconds",
            "start offset seconds",
            "start seconds",
            "event start offset seconds",
            "start offset",
        };

        public static readonly IEnumerable<string> EndNames = new string[]
        {
            "end",
            "event end seconds",
            "end offset seconds",
            "end seconds",
            "event end offset seconds",
            "end offset",
        };
        public static readonly IEnumerable<string> LowNames = new string[]
        {
            "low",
            "event low hertz",
            "low frequency hertz",
            "low hertz",
            "min hz"
        };
        public static readonly IEnumerable<string> CentroidLowNames = new string[]
        {
            "frequency",
            "centroid frequency",
            "hertz",
            "low",
            "event low hertz",
            "low hertz",
        };
        public static readonly IEnumerable<string> HighNames = new string[]
        {
            "high",
            "event high hertz",
            "high frequency hertz",
            "high hertz",
            "max hz"
        };

        public static readonly IEnumerable<string> BandWidthNames = new string[]
        {
            "bandwidth",
            "bandwidth hertz"
        };

        public static readonly IEnumerable<string> DurationNames = new string[]
        {
            "duration",
            "event duration",
            "duration seconds",
            "event duration seconds"
        };

        public static readonly IReadOnlyDictionary<string, Func<string, string>> NamingConventions = new Dictionary<string, Func<string, string>>() {
            {
                "title case",
                memo((string x) => x.Split(' ').Select( x => char.ToUpperInvariant(x[0]) + x[1..]).Join(" ") )},
            {
                "snake case",
                memo((string x) => x.Replace(" ", "_")) },
            {
                "pascal case",
                memo((string x) => x.Split(' ').Select( x => char.ToUpperInvariant(x[0]) + x[1..]).JoinWithoutGap())
            },
        };

        private static readonly string ConventionList = NamingConventions.Keys.JoinWithComma();

        private static IEnumerable<string> GenerateNames(string name)
        {
            foreach (var (_, convention) in NamingConventions)
            {
                yield return convention(name);
            }
        }

        public static Validation<string, KeyedValue<T>> TryNames<T>(ITryGetValue subject, IEnumerable<string> names)
        {
            // first try every name defined in the naming conventions

            var allNames = names.SelectMany(GenerateNames).Concat(names).ToArray();
            foreach (var name in allNames)
            {
                if (subject.TryGetValue<T>(name, out var matchedValue, StringComparison.InvariantCulture))
                {
                    return new KeyedValue<T>(name, matchedValue);
                }
            }

            // as a last ditch effort try names again, this time case-invariant
            foreach (var name in allNames)
            {
                if (subject.TryGetValue<T>(name, out var matchedValue, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new KeyedValue<T>(name, matchedValue);
                }
            }

            return $"Could not find a property with a name like `{names.First()}`. Checked { allNames.Join(", ", "`") }";
        }
    }
}