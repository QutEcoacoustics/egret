using Egret.Cli.Models;
using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Egret.Cli.Serialization
{
    public class ExpectationTypeResolver : ITypeDiscriminator
    {
        private readonly Dictionary<string, Type> typeLookup;

        public ExpectationTypeResolver(INamingConvention namingConvention)
        {
            typeLookup = new Dictionary<string, Type>() {
                { namingConvention.Apply(nameof(BoundedExpectation.Bounds)), typeof(BoundedExpectation) },
                { namingConvention.Apply(nameof(CentroidExpectation.Centroid)), typeof(CentroidExpectation) },
                { namingConvention.Apply(nameof(TemporalExpectation.Time)), typeof(TemporalExpectation) },
            };
        }

        public Type BaseType => typeof(IExpectation);

        public bool TryResolve(ParsingEventBuffer buffer, out Type suggestedType)
        {
            if (buffer.TryFindMappingEntry(
                scalar => typeLookup.ContainsKey(scalar.Value),
                out Scalar key,
                out ParsingEvent _))
            {
                suggestedType = typeLookup[key.Value];
                return true;
            }

            suggestedType = null;
            return false;
        }
    }
}