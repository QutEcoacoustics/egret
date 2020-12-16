using Egret.Cli.Serialization.Json;
using LanguageExt;
using System;
using System.Linq;
using System.Text.Json;
using static LanguageExt.Prelude;

namespace Egret.Cli.Models
{
    public class JsonResult : NormalizedResult
    {
        private readonly JsonElement data;
        private readonly string[] names;

        public JsonResult(JsonElement data, SourceInfo sourceInfo)
        {
            this.SourceInfo = sourceInfo;
            if (data.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("must be a JSON Object", nameof(data));
            }

            // break the reference to the underlying file stream,
            // which on close would dispose the memory backing the JsonElement
            this.data = data.Clone();
            // this is not a great solution, but none of the inbuilt mechanism for creating lookups for string keys
            // support a StringComparison argument when checking if a key exists!
            names = data.EnumerateObject().Select(property => property.Name).ToArray();
        }

        public override SourceInfo SourceInfo { get; set; }

        public override bool TryGetValue<T>(string key, out T value, StringComparison comparison = StringComparison.InvariantCulture)
        {


            var propExists = names.Find(name => name.Equals(key, comparison));
            if (propExists)
            {
                var element = data.GetProperty((string)propExists);

                // a generic coverion method is coming in a later version of system.json
                var typeArg = typeof(T);
                try
                {
                    if (typeArg == typeof(double))
                    {
                        value = (T)(object)element.GetDouble();
                    }
                    else if (typeArg == typeof(string))
                    {
                        value = (T)(object)element.GetString();
                    }
                    else
                    {
                        throw new NotImplementedException($"Support for get type {typeArg} from a JsonElement is not yet implemented");
                    }
                }
                catch (InvalidOperationException invalid)
                {
                    // TODO: turn this into nice error message and flow it through
                    throw new InvalidOperationException(
                        $"Could not fetch key `{key}` for type `{typeArg}` from `{element.GetRawText()}` (a {element.ValueKind})",
                        invalid);
                }

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