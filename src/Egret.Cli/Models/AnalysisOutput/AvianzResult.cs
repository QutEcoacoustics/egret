using Egret.Cli.Extensions;
using Egret.Cli.Models.Avianz;
using Egret.Cli.Serialization.Json;
using LanguageExt;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static LanguageExt.Prelude;

namespace Egret.Cli.Models.AnalysisOutput
{
    public class AvianzResult : NormalizedResult
    {
        private static readonly ConcurrentDictionary<string, Func<Annotation, object>> Lookup = new();
        private static readonly IReadOnlyCollection<string> propertyNames;

        private readonly Annotation annotation;

        static AvianzResult()
        {
            // use reflection one to generate member accessors
            var properties = typeof(Annotation).GetProperties();
            var names = new string[properties.Length];
            uint index = 0;
            foreach (var property in properties)
            {
                var getter = property.BuildUntypedGetter<Annotation>();

                Lookup.TryAdd(property.Name, getter);
                names[index] = property.Name;
                index++;
            }
            propertyNames = names;
        }

        public AvianzResult(Annotation data, SourceInfo sourceInfo)
        {
            SourceInfo = sourceInfo;
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            annotation = data;
        }
        public override SourceInfo SourceInfo { get; set; }

        public override bool TryGetValue<T>(string key, out T value, StringComparison comparison = StringComparison.InvariantCulture)
        {
            var propertyExists = propertyNames.Find(name => name.Equals(key, comparison));
            if (propertyExists)
            {
                var intermediate = Lookup[(string)propertyExists].Invoke(annotation);

                value = intermediate switch
                {
                    Arr<Label> a => (T)a.Select(l => l.Species).AsEnumerable(),
                    _ => (T)intermediate
                };
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