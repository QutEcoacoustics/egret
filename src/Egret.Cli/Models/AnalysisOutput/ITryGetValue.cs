using System;

namespace Egret.Cli.Models
{
    public interface ITryGetValue
    {
        bool TryGetValue<T>(string key, out T value, StringComparison comparison = StringComparison.InvariantCulture);
    }
}