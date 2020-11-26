
using Egret.Cli.Serialization;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Egret.Cli.Models
{
    public class Tool : IKeyedObject
    {
        private string versionPattern;

        public string Executable { get; init; }
        public string Command { get; init; }

        public string ResultPattern { get; init; }

        public string VersionPattern
        {
            get
            {
                return versionPattern;
            }
            init
            {
                versionPattern = value;
                VersionRegex = new Regex(versionPattern, RegexOptions.Compiled);
            }
        }

        [YamlIgnore]
        public Regex VersionRegex { get; init; }

        public string Name => ((IKeyedObject)this).Key;

        string IKeyedObject.Key { get; set; }
    }
}