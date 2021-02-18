using System;
using System.IO.Abstractions;

namespace Egret.Cli.Processing
{
    /// <summary>
    /// Bridges System.IO.Abstractions and Microsoft.Extensions.FileSystemGlobbing for file info. Based off of:
    /// https://github.com/dotnet/runtime/blob/9c5d363bf8903e269284e660875e8fae0c1b9a79/src/libraries/Microsoft.Extensions.FileSystemGlobbing/src/Abstractions/FileInfoWrapper.cs
    /// </summary>
    public class FileInfoBaseAbstractionAdapter : Microsoft.Extensions.FileSystemGlobbing.Abstractions.FileInfoBase
    {
        private readonly IFileInfo file;

        public FileInfoBaseAbstractionAdapter(IFileInfo file)
        {
            this.file = file;
        }

        public override string Name => file.Name;

        public override string FullName => file.FullName;

        public override Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoBase ParentDirectory
            => new DirectoryInfoBaseAbstractionAdapter(file.Directory);
    }
}