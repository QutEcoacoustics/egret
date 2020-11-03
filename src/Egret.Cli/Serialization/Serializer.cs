using Egret.Cli.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Egret.Cli.Serialization
{
    public class Serializer
    {
        private ILogger<Serializer> Logger { get; }
        public Serializer(ILogger<Serializer> logger)
        {
            this.Logger = logger;
            this.YamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithTagMapping("!no_events", typeof(NoEvents))
                .WithTagMapping("!event_count", typeof(EventCount))
                .WithTypeConverter(new IntervalTypeConverter())
                // WithTypeResolver
                // WithTypeConverter
                // WithObjectFactory
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
            Normalize(config);

            // TODO: call validation?

            Logger.LogDebug("Finished loading config file: {file}", configFile);
            return config;
        }

        private static void Normalize(Config original)
        {
            foreach (var (name, suite) in original.TestSuites)
            {
                // stick the dictionary key in the name field if no name was provided.
                // We expect this to be the default case.
                if (string.IsNullOrWhiteSpace(suite.Name))
                {
                    suite.Name = name;
                }

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