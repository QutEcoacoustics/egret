using Egret.Cli.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Egret.Cli.Processing
{
    public class TempFactory : IDisposable
    {
        private readonly string basePath;
        private readonly LinkedList<string> filesCreated = new();
        private readonly SortedDictionary<int, HashSet<string>> dirsCreated = new();
        private readonly ILogger<TempFactory> logger;
        private readonly IFileSystem fileSystem;

        public TempFactory(ILogger<TempFactory> logger, IFileSystem fileSystem)
            : this(logger, fileSystem, null)
        {
        }

        public TempFactory(ILogger<TempFactory> logger, IFileSystem fileSystem, string basePath)
        {
            this.fileSystem = fileSystem;
            this.logger = logger;

            if (basePath is null)
            {
                basePath = fileSystem.Path.Join(fileSystem.Path.GetTempPath(), "egret");
            }
            else
            {
                basePath = fileSystem.Path.GetFullPath(basePath);
            }

            fileSystem.Directory.CreateDirectory(basePath);

            this.basePath = basePath;
        }

        public IFileInfo GetTempFileWithExt(string stem = null, string extension = null, bool create = false)
        {
            var filename = fileSystem.Path.GetRandomFileName();
            filename = (stem ?? filename[0..7]) + '.' + (extension.TrimStart('.') ?? filename[8..11]);

            return GetTempFile(filename, create: create);
        }
        public IFileInfo GetTempFile(string filename = null, bool create = false)
        {
            var fileName = filename ?? fileSystem.Path.GetRandomFileName();

            var path = fileSystem.Path.Join(basePath, fileName);

            return AddFile(path, create);
        }

        public IFileInfo GetTempFile(string[] fragments, bool create = false)
        {
            var (dirs, filename) = fragments?.Length switch
            {
                null or 0 => (basePath, fileSystem.Path.GetRandomFileName()),
                1 => (basePath, fragments[0]),
                _ => (fileSystem.Path.Combine(basePath, fileSystem.Path.Combine(fragments[0..^2])), fragments[^1])
            };

            var path = fileSystem.Path.Join(dirs, filename);

            AddDirectory(Math.Max(0, (fragments?.Length ?? 0) - 1), dirs);

            return AddFile(path, create);
        }

        public IDirectoryInfo GetTempDir(params string[] directories)
        {

            if (directories is null or Array { Length: 0 })
            {
                directories = new[] { fileSystem.Path.GetRandomFileName() };
            }

            var path = fileSystem.Path.Join(basePath, fileSystem.Path.Join(directories));

            AddDirectory(directories.Length, path);

            return fileSystem.DirectoryInfo.FromDirectoryName(path);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            foreach (var path in filesCreated)
            {
                try
                {
                    fileSystem.File.Delete(path);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete temp file: {file}", path);
                }
            }

            // delete temp dirs from most nested out to least nested
            foreach (var (_, list) in dirsCreated.Reverse())
            {
                foreach (var path in list)
                {
                    try
                    {
                        fileSystem.Directory.Delete(path, true);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to delete temp directory: {directory}", path);
                    }
                }
            }
        }

        private void AddDirectory(int depth, string path)
        {
            dirsCreated.TryAdd(depth, new());
            if (dirsCreated[depth].Add(path))
            {
                fileSystem.Directory.CreateDirectory(path);
            }
        }

        private IFileInfo AddFile(string path, bool create)
        {
            filesCreated.AddLast(path);

            var file = fileSystem.FileInfo.FromFileName(path);
            if (create)
            {
                file.Create();
            }

            return file;
        }
    }
}