using Egret.Cli.Models;
using System;
using System.Collections.Generic;

namespace Egret.Tests.Models.AnalysisResults
{

    public class DictionaryResult : NormalizedResult
    {
        private readonly IDictionary<string, object> labels;
        private readonly ICollection<string> keys;

        public DictionaryResult(int index, IDictionary<string, object> labels) : base(index)
        {
            this.labels = labels;
            keys = labels.Keys;
        }

        public override SourceInfo SourceInfo { get; set; } = SourceInfo.Unknown;

        public override bool TryGetValue<T>(string key, out T value, StringComparison comparison = StringComparison.InvariantCulture)
        {
            var foundKey = keys.Find(k => k.Equals(key, comparison));
            if (foundKey)
            {
                var resultValue = labels[(string)foundKey];
                value = (T)resultValue;
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