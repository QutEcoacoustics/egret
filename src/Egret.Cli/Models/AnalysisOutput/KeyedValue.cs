using System;
using System.Collections.Generic;

namespace Egret.Cli.Models
{
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

}