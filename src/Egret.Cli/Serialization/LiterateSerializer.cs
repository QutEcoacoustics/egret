using Egret.Cli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.Utilities;

namespace Egret.Cli.Serialization
{
    public class LiterateSerializer
    {
        private readonly ISerializer YamlSerializer;

        public LiterateSerializer(IOptions<AppSettings> settings)
        {
            this.YamlSerializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithEventEmitter(next => new FlowEverythingEmitter(next))
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
                .WithTypeConverter(new ShortDoubleConverter())
                .WithTypeConverter(new IntervalTypeConverter(settings.Value.DefaultThreshold, simplify: true, "0.##"))
                .WithAttributeOverride<IExpectation>(x => x.Name, new YamlIgnoreAttribute())
                .Build();

        }

        public string OneLine<T>(T @object)
        {
            return this.YamlSerializer.Serialize(@object);
        }

        private class FlowEverythingEmitter : ChainedEventEmitter
        {
            public FlowEverythingEmitter(IEventEmitter nextEmitter) : base(nextEmitter) { }

            public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
            {
                eventInfo.Style = MappingStyle.Flow;
                base.Emit(eventInfo, emitter);
            }

            public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
            {
                eventInfo.Style = SequenceStyle.Flow;
                nextEmitter.Emit(eventInfo, emitter);
            }
        }

        private class ShortDoubleConverter : IYamlTypeConverter
        {
            public bool Accepts(Type type)
            {
                return type == typeof(double);
            }

            public object ReadYaml(IParser parser, Type type)
            {
                throw new NotImplementedException();
            }

            public void WriteYaml(IEmitter emitter, object value, Type type)
            {
                emitter.Emit(
                    new Scalar(
                    ((double)value).ToString("0.##"))
                );
            }
        }
    }
}