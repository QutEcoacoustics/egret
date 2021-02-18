using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using static LanguageExt.Prelude;


namespace Egret.Cli.Processing
{
    public class PathResolver
    {
        public static IEnumerable<Fin<string>> ResolvePathOrGlob(
            IFileSystem fileSystem,
            string pathOrPattern,
            string workingDirectory)
        {
            if (pathOrPattern is null or "")
            {
                // can't match nothing
                yield break;
            }

            if (fileSystem.Path.IsPathFullyQualified(pathOrPattern) || fileSystem.Path.IsPathRooted(pathOrPattern))
            {
                if (fileSystem.File.Exists(pathOrPattern) || fileSystem.Directory.Exists(pathOrPattern))
                {
                    yield return fileSystem.Path.GetFullPath(pathOrPattern);
                }
                else
                {
                    yield return Error.New($"Path`{pathOrPattern}` does not exist. Is the path correct?");
                }
                yield break;
            }

            Fin<MultiGlob> glob;
            try
            {
                glob = MultiGlob.Parse(pathOrPattern);
            }
            catch (ArgumentException aex)
            {
                // can't yield in a catch clause, hence the whole song and dance
                glob = Error.New(aex);
            }

            if (glob.Case is Error e)
            {
                yield return e;
                yield break;
            }

            var count = 0;
            foreach (var result in glob.ThrowIfFail().GetResultsInFullPath(fileSystem, workingDirectory))
            {
                yield return result;
                count++;
            }

            if (count == 0)
            {
                yield return Error.New(
                    $"Expanding pattern `{pathOrPattern}` produced no files. Is that pattern correct?");
            }
        }
    }
}