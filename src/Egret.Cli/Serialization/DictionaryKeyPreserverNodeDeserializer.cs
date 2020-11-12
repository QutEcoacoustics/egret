using System;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;

namespace Egret.Cli.Serialization
{
    public interface IKeyedObject
    {
        string Key { get; set; }
    }

    public class DictionaryKeyPreserverNodeDeserializer : INodeDeserializer
    {
        private readonly INodeDeserializer original;

        public DictionaryKeyPreserverNodeDeserializer(INodeDeserializer original)
        {
            this.original = original;
        }
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (original.Deserialize(reader, expectedType, nestedObjectDeserializer, out value))
            {
                if (value is IDictionary dictionary)
                {
                    foreach (DictionaryEntry kvp in dictionary)
                    {
                        if (kvp.Key is string key && kvp.Value is IKeyedObject keyed)
                        {
                            keyed.Key = key;
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }
}