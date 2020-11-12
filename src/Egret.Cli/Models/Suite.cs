
using Egret.Cli.Serialization;
using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    public class Suite : IKeyedObject
    {
        private readonly string name;

        public string Name
        {
            get
            {
                return name ?? ((IKeyedObject)this).Key;
            }
            init
            {
                name = value;
            }
        }


        public string DefaultLabel { get; init; }

        public Case[] Tests { get; init; }

        public string[] IncludeCases { get; init; } = Array.Empty<string>();

        public Dictionary<string, Case[]> SharedCases { get; init; } = new();

        public Dictionary<string, Dictionary<string, object>> ToolConfigs { get; init; }

        string IKeyedObject.Key { get; set; }

        /// <summary>
        /// Stores the FileInfo (and path) to the file from which this config was read.
        /// </summary>
        /// <value>he FileInfo for the file from which this config was read.</value>
        /// <remarks>
        /// This is used to resolve relative paths to files from inside the config files.
        /// </remarks>
        [YamlIgnore]
        public string Location { get; internal set; }


    }




}