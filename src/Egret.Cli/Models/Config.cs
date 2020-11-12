
using StringTokenFormatter;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.Unicode;
using YamlDotNet.Core.Tokens;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    public class Config
    {
        public Dictionary<string, Tool> Tools { get; init; }

        public Dictionary<string, Suite> TestSuites { get; init; }

        public Dictionary<string, Case[]> CommonCases { get; init; }

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