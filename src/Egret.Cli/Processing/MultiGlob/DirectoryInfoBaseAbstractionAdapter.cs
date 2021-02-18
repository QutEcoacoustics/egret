using System;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace Egret.Cli.Processing
{
    /// <summary>
    /// Bridges System.IO.Abstractions and Microsoft.Extensions.FileSystemGlobbing for directory info. Based off of:
    /// https://github.com/dotnet/runtime/blob/9c5d363bf8903e269284e660875e8fae0c1b9a79/src/libraries/Microsoft.Extensions.FileSystemGlobbing/src/Abstractions/DirectoryInfoWrapper.cs
    /// </summary>
    public class DirectoryInfoBaseAbstractionAdapter : Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoBase
    {
        private readonly IDirectoryInfo directory;
        private readonly bool isParentPath;

        public DirectoryInfoBaseAbstractionAdapter(IDirectoryInfo directory) : this(directory, false)
        {
        }

        public DirectoryInfoBaseAbstractionAdapter(IDirectoryInfo directory, bool isParentPath)
        {
            this.directory = directory;

            this.isParentPath = isParentPath;
        }

        public override string Name => isParentPath ? ".." : directory.Name;

        public override string FullName => directory.FullName;

        public override Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoBase ParentDirectory =>
            new DirectoryInfoBaseAbstractionAdapter(directory.Parent);

        public override IEnumerable<Microsoft.Extensions.FileSystemGlobbing.Abstractions.FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            if (directory.Exists)
            {
                IEnumerable<IFileSystemInfo> fileSystemInfos;
                try
                {
                    fileSystemInfos = directory.EnumerateFileSystemInfos("*", System.IO.SearchOption.TopDirectoryOnly);
                }
                catch (System.IO.DirectoryNotFoundException)
                {
                    yield break;
                }

                foreach (IFileSystemInfo fileSystemInfo in fileSystemInfos)
                {
                    if (fileSystemInfo is IDirectoryInfo directoryInfo)
                    {
                        yield return new DirectoryInfoBaseAbstractionAdapter(directoryInfo);
                    }
                    else
                    {
                        yield return new FileInfoBaseAbstractionAdapter((IFileInfo)fileSystemInfo);
                    }
                }
            }
        }

        public override Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoBase GetDirectory(string path)
        {
            bool isParentPath = string.Equals(path, "..", StringComparison.Ordinal);

            if (isParentPath)
            {
                return new DirectoryInfoBaseAbstractionAdapter(
                    directory.FileSystem.DirectoryInfo.FromDirectoryName(directory.FileSystem.Path.Combine(directory.FullName, path)),
                    isParentPath);
            }
            else
            {
                IDirectoryInfo[] dirs = directory.GetDirectories(path);

                if (dirs.Length == 1)
                {
                    return new DirectoryInfoBaseAbstractionAdapter(dirs[0], isParentPath);
                }
                else if (dirs.Length == 0)
                {
                    return null;
                }
                else
                {
                    // This shouldn't happen. The parameter path isn't supposed to contain wild card.
                    throw new InvalidOperationException(
                        string.Format("More than one sub directories are found under {0} with path {1}.",
                            directory.FullName, path));
                }
            }
        }

        public override Microsoft.Extensions.FileSystemGlobbing.Abstractions.FileInfoBase GetFile(string path)
            => new FileInfoBaseAbstractionAdapter(directory.FileSystem.FileInfo.FromFileName(path));
    }
}