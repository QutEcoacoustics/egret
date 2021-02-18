using System.Linq;

namespace Egret.Tests.Support
{
    public record TestFile
    {
        public TestFile(string path, string contents)
        {
            Path = path;
            Contents = contents;
        }

        public string Path { get; init; }
        public string Contents { get; init; }

        public static implicit operator TestFile((string, string) pair)
        {
            return new TestFile(pair.Item1, pair.Item2);
        }

        public override string ToString() => $"{Path} <Length: {Contents.Length}>";
    }

}