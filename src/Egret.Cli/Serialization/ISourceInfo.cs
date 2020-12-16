using Egret.Cli.Models;
using YamlDotNet.Serialization;

namespace Egret.Cli.Serialization.Yaml
{
    public interface ISourceInfo
    {
        [YamlIgnore]
        public SourceInfo SourceInfo { get; set; }
    }
}