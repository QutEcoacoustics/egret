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
        private readonly KeyValuePair<string, Type> fallback;

        public ExpectationTypeResolver(INamingConvention namingConvention)
        {
            typeLookup = new Dictionary<string, Type>() {
                { namingConvention.Apply(nameof(BoundedExpectation.Bounds)), typeof(BoundedExpectation) },
                { namingConvention.Apply(nameof(CentroidExpectation.Centroid)), typeof(CentroidExpectation) },
                { namingConvention.Apply(nameof(TemporalExpectation.Time)), typeof(TemporalExpectation) },
            };

            // Only match label only expectation as a last resort. We want more specific matchers to match first.
            fallback = new KeyValuePair<string, Type>(namingConvention.Apply(nameof(LabelOnlyExpectation.Label)), typeof(LabelOnlyExpectation));
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

            buffer.Reset();
            if (buffer.TryFindMappingEntry(
                scalar => fallback.Key == scalar.Value,
                out Scalar _,
                out ParsingEvent _))
            {
                suggestedType = fallback.Value;
                return true;
            }

            suggestedType = null;
            return false;
        }
    }
}