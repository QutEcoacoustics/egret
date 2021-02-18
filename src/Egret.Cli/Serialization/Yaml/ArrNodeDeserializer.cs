namespace Egret.Cli.Serialization.Yaml
{
    using LanguageExt;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using YamlDotNet.Core;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.Utilities;

    public class ArrNodeDeserializer : INodeDeserializer
    {
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object value)
        {
            if (expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == typeof(Arr<>))
            {

                var itemsType = expectedType.GetGenericArguments();
                var collectionType = typeof(List<>).MakeGenericType(itemsType);
                var result = nestedObjectDeserializer(reader, collectionType);


                var resultType = typeof(Arr<>).MakeGenericType(itemsType);
                value = Activator.CreateInstance(resultType, result);

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