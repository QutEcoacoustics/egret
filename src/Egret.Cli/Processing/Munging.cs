
using Egret.Cli.Models;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YamlDotNet.Core.Tokens;

namespace Egret.Cli.Processing
{
    public class Munging
    {

        public static readonly IEnumerable<string> LabelNames = new string[]
        {
            "label",
            "tag",
            "species",
            "name"
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
            { "title case" , (x) =>  string.Join(' ', x.Split(' ').Select( x => char.ToUpperInvariant(x[0]) + x[1..])) },
            { "snake case" , (x) => x.Replace(" ", "_") },
            {
                "pascal case",
                (x) => string.Join(string.Empty, x.Split(' ').Select( x => char.ToUpperInvariant(x[0]) + x[1..]))
            },
        };

        private static readonly string ConventionList = string.Join(",", NamingConventions.Keys);

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

            return $"Could not find any property that matched the name label. Checked {names} and all {ConventionList} variants";
        }
    }
}