using Egret.Cli.Models;
using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Egret.Cli.Serialization.Yaml
{


    public class AliasedStringNodeDeserializer : INodeDeserializer
    {
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (expectedType == typeof(AliasedString))
            {
                var result = nestedObjectDeserializer(reader, typeof(List<string>));
                value = new AliasedString(result as IEnumerable<string>);
                return true;
            }
            else
            {
                value = null;
                return false;
            }

        }
    }
}