
using System.IO;
using System.Linq;

namespace Egret.Cli.Extensions
{
    public static class FileSystemInfoExtensions
    {
        public static FileInfo Combine(this DirectoryInfo info, params string[] segments)
        {
            var rest = Path.Combine(segments);
            return new FileInfo(Path.Combine(info.FullName, rest));
        }

        public static string Filestem(this FileInfo info)
        {
            return Path.GetFileNameWithoutExtension(info.Name);
        }
    }
}