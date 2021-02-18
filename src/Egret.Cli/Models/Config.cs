using System.Collections.Generic;
using System.IO.Abstractions;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    public class Config
    {
        public Dictionary<string, Tool> Tools { get; init; } = new Dictionary<string, Tool>();

        public Dictionary<string, Suite> TestSuites { get; init; } = new Dictionary<string, Suite>();

        public Dictionary<string, TestCase[]> CommonTests { get; init; } = new Dictionary<string, TestCase[]>();

        /// <summary>
        /// Stores the FileInfo (and path) to the file from which this config was read.
        /// </summary>
        /// <value>he FileInfo for the file from which this config was read.</value>
        /// <remarks>
        /// This is used to resolve relative paths to files from inside the config files.
        /// </remarks>
        [YamlIgnore]
        public IFileInfo Location { get; internal set; }
    }
}