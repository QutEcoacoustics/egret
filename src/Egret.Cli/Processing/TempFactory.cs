using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Egret.Cli.Processing
{
    public class TempFactory : IDisposable
    {
        private readonly string basePath;
        private readonly LinkedList<string> filesCreated = new();
        private readonly SortedDictionary<int, HashSet<string>> dirsCreated = new();
        private readonly ILogger<TempFactory> logger;

        public TempFactory(ILogger<TempFactory> logger) : this(logger, null)
        {
        }

        public TempFactory(ILogger<TempFactory> logger, string basePath)
        {
            this.logger = logger;

            if (basePath is null)
            {
                basePath = Path.Join(Path.GetTempPath(), "egret");
            }
            else
            {
                basePath = Path.GetFullPath(basePath);
            }

            Directory.CreateDirectory(basePath);

            this.basePath = basePath;
        }

        public FileInfo GetTempFileWithExt(string stem = null, string extension = null, bool create = false)
        {
            var filename = Path.GetRandomFileName();
            filename = (stem ?? filename[0..7]) + '.' + (extension.TrimStart('.') ?? filename[8..11]);

            return GetTempFile(filename, create: create);
        }
        public FileInfo GetTempFile(string filename = null, bool create = false)
        {
            var fileName = filename ?? Path.GetRandomFileName();

            var path = Path.Join(basePath, fileName);

            return AddFile(path, create);
        }

        public FileInfo GetTempFile(string[] fragments, bool create = false)
        {
            var (dirs, filename) = fragments?.Length switch
            {
                null or 0 => (basePath, Path.GetRandomFileName()),
                1 => (basePath, fragments[0]),
                _ => (Path.Combine(basePath, Path.Combine(fragments[0..^2])), fragments[^1])
            };

            var path = Path.Join(dirs, filename);

            AddDirectory(Math.Max(0, (fragments?.Length ?? 0) - 1), dirs);

            return AddFile(path, create);
        }

        public DirectoryInfo GetTempDir(params string[] directories)
        {

            if (directories is null or Array { Length: 0 })
            {
                directories = new[] { Path.GetRandomFileName() };
            }

            var path = Path.Join(basePath, Path.Join(directories));

            AddDirectory(directories.Length, path);

            return new DirectoryInfo(path);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            foreach (var path in filesCreated)
            {
                try
                {
                    File.Delete(path);
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
                        Directory.Delete(path, true);
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
                Directory.CreateDirectory(path);
            }
        }

        private FileInfo AddFile(string path, bool create)
        {
            filesCreated.AddLast(path);

            var file = new FileInfo(path);
            if (create)
            {
                file.Create();
            }

            return file;
        }
    }
}