
using Egret.Cli.Serialization;

namespace Egret.Cli.Models
{
    public class Tool : IKeyedObject
    {
        public string Executable { get; init; }
        public string Command { get; init; }

        public string ResultPattern { get; init; }

        public string Name => ((IKeyedObject)this).Key;

        string IKeyedObject.Key { get; set; }
    }




}