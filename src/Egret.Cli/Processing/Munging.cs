
using Egret.Cli.Models;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core.Tokens;

namespace Egret.Cli.Processing
{
    public class Munging
    {

        public static readonly IEnumerable<string> LabelNames = new string[]
        {
            "Label",
            "Tag",
            "Species",
            "Name"
        };

        public static readonly IReadOnlyDictionary<string, Func<string, string>> NamingConventions = new Dictionary<string, Func<string, string>>() {
            { "lower case" , (x) => x.ToLower()},
        };

        private static readonly string ConventionList = string.Join(",", NamingConventions.Keys);

        private static IEnumerable<string> GenerateNames(string name)
        {
            foreach (var (_, convention) in NamingConventions)
            {
                yield return convention(name);
            }
        }

        public static Validation<string, (string Key, T Value)> TryNames<T>(ITryGetValue subject, IEnumerable<string> names)
        {
            // first try names, and then try every other name defined in the naming conventions
            foreach (var name in names.Concat(names.SelectMany(GenerateNames)))
            {
                if (subject.TryGetValue<T>(name, out var matchedValue))
                {
                    return (name, matchedValue);
                }
            }

            return $"Could not find any property that matched the name label. Checked {names} and all {ConventionList} variants";
        }
    }
}