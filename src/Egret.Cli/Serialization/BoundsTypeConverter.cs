using Egret.Cli.Models;
using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Egret.Cli.Serialization
{
    public class BoundsTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return type == typeof(Bounds)
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var value = parser.
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }
    }
}