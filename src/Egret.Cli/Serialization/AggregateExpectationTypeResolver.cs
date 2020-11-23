using Egret.Cli.Models;
using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

using YamlDotNet.Serialization;

namespace Egret.Cli.Serialization
{
    public class AggregateExpectationTypeResolver : ITypeDiscriminator
    {
        public const string TargetKey = nameof(AggregateExpectation.SegmentWith);
        private readonly string targetKey;
        private readonly Dictionary<string, Type> typeLookup;

        public AggregateExpectationTypeResolver(INamingConvention namingConvention)
        {
            targetKey = namingConvention.Apply(TargetKey);
            typeLookup = new Dictionary<string, Type>() {
                 { namingConvention.Apply(nameof(NoEvents)), typeof(NoEvents) },
                 { namingConvention.Apply(nameof(EventCount)), typeof(EventCount) },
            };
        }
        public Type BaseType => typeof(IExpectationTest);

        public bool TryResolve(ParsingEventBuffer buffer, out Type suggestedType)
        {
            if (buffer.TryFindMappingEntry(
                scalar => targetKey == scalar.Value,
                out Scalar key,
                out ParsingEvent value))
            {
                // read the value of the kind key
                if (value is Scalar valueScalar)
                {
                    suggestedType = CheckName(valueScalar.Value);

                    return true;
                }
                else
                {
                    FailEmpty();
                }
            }

            // we could not find our key, thus we could not determine correct child type
            suggestedType = null;
            return false;
        }


        private void FailEmpty()
        {
            throw new Exception($"Could not determin expectation type, {targetKey} has an empty value");
        }

        private Type CheckName(string value)
        {
            if (typeLookup.TryGetValue(value, out var childType))
            {
                return childType;
            }

            var known = string.Join(", ", typeLookup.Keys);
            throw new Exception($"Could not match `{targetKey}: {value} to a known expectation. Expecting one of: {known}");
        }
    }
}