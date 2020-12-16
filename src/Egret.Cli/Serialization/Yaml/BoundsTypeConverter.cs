using Egret.Cli.Models;
using Serilog.Events;
using System;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Egret.Cli.Serialization
{
    // public class BoundsTypeConverter : IYamlTypeConverter
    // {
    //     public bool Accepts(Type type)
    //     {
    //         return type == typeof(Bounds);
    //     }

    //     public object ReadYaml(IParser parser, Type type)
    //     {
    //         parser.Consume<SequenceStart>();

    //         parser.Consume<Scalar>();
    //         this.StartSeconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

    //         parser.Consume<Scalar>();
    //         this.EndSeconds = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

    //         parser.Consume<Scalar>();
    //         this.LowHertz = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

    //         parser.Consume<Scalar>();
    //         this.HighHertz = (Interval)nestedObjectDeserializer.Invoke(typeof(Interval));

    //         parser.Consume<SequenceEnd>();
    //     }

    //     public void WriteYaml(IEmitter emitter, object value, Type type)
    //     {
    //         throw new NotImplementedException();
    //     }
    // }



    public class IntervalTypeConverter : IYamlTypeConverter
    {
        private readonly bool simplify;
        private readonly string endpointFormat;

        public double DefaultThreshold { get; }
        public IntervalTypeConverter(double defaultThreshold, bool simplify = false, string endpointFormat = null)
        {
            this.DefaultThreshold = defaultThreshold;
            this.simplify = simplify;
            this.endpointFormat = endpointFormat;
        }

        public bool Accepts(Type type)
        {
            return type == typeof(Interval);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var scalar = parser.Consume<Scalar>();
            var bytes = Encoding.UTF8.GetBytes(scalar.Value);
            return Interval.FromString(bytes, DefaultThreshold);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            emitter.Emit(new Scalar(((Interval)value).ToString(simplify: simplify, endpointFormat)));
        }
    }
}