using Egret.Cli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Egret.Cli.Serialization
{
    public class Deserializer
    {
        private ILogger<Deserializer> Logger { get; }

        private readonly INamingConvention NamingConvention;

        public Deserializer(ILogger<Deserializer> logger, IOptions<AppSettings> settings)
        {
            Logger = logger;
            NamingConvention = UnderscoredNamingConvention.Instance;

            // these resolvers allow us to deserialize to an abstract class or interface
            var aggregateExpectationResolver = new AggregateExpectationTypeResolver(NamingConvention);
            var expectationResolver = new ExpectationTypeResolver(NamingConvention);

            YamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(NamingConvention)
                .WithTypeConverter(new IntervalTypeConverter(settings.Value.DefaultThreshold))
                .WithNodeDeserializer(
                    inner => new AbstractNodeNodeTypeResolver(inner, aggregateExpectationResolver, expectationResolver),
                     s => s.InsteadOf<ObjectNodeDeserializer>())
                .WithNodeDeserializer(
                    inner => new DictionaryKeyPreserverNodeDeserializer(inner),
                     s => s.InsteadOf<DictionaryNodeDeserializer>())
                // more: https://github.com/aaubry/YamlDotNet/wiki/Serialization.Deserializer
                .Build();
        }

        public IDeserializer YamlDeserializer { get; private set; }

        public Config Deserialize(FileInfo configFile)
        {
            Logger.LogDebug("Loading config file: {file}", configFile);
            using var reader = configFile.OpenText();
            var config = this.YamlDeserializer.Deserialize<Config>(reader);

            Logger.LogDebug("Normalizing config file: {file}", configFile);
            Normalize(config, configFile.FullName);

            // TODO: call validation?

            Logger.LogDebug("Finished loading config file: {file}", configFile);
            return config;
        }

        private static void Normalize(Config original, string filePath)
        {
            original.Location = filePath;

            foreach (var (name, suite) in original.TestSuites)
            {
                suite.Location = filePath;
                // resolve includes
                if (suite.IncludeCases.Length > 0)
                {
                    foreach (var include in suite.IncludeCases)
                    {
                        // cases are in this file
                        if (original.CommonCases.TryGetValue(include, out var cases))
                        {
                            suite.SharedCases.Add(include, cases);
                            continue;
                        }

                        // cases are in another file: try to load file
                        // TODO 

                        throw new Exception($"Cannot load includes: {name} was not found");

                    }
                }
            }
        }
    }
}