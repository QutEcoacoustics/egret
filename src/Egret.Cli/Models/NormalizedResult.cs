
using Egret.Cli.Processing;
using LanguageExt;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;


namespace Egret.Cli.Models
{
    public interface ITryGetValue
    {
        bool TryGetValue<T>(string key, out T value);
    }

    public abstract class NormalizedResult : ITryGetValue
    {

        public abstract bool TryGetValue<T>(string key, out T value);

        private Validation<string, (string Key, string Value)> label;

        public Validation<string, (string Key, string Value)> Label
        {
            get
            {
                if (label == default)
                {
                    label = Munging.TryNames<string>(this, Munging.LabelNames);
                }

                return label;
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

        public override bool TryGetValue<T>(string key, out T value)
        {

            var propExists = source.TryGetValue(key, StringComparison.InvariantCulture, out var valueElement);
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