using System;

namespace Egret.Cli.Models
{
    public interface ITypeDiscriminator
    {
        Type BaseType { get; }

        bool TryResolve(ParsingEventBuffer buffer, out Type suggestedType);
    }
}