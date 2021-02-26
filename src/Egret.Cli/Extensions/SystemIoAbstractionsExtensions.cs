using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Egret.Cli.Extensions
{
    public static class SystemIoAbstractionsExtensions
    {
        /// <summary>
        /// Polyfill for <c>Path.Join(params string[] paths)</c>.
        /// Reference: https://github.com/dotnet/runtime/blob/a9c5eadd951dcba73167f72cc624eb790573663a/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs#L473
        /// </summary>
        public static string Join(this IPath host, params string[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            if (paths.Length == 0)
            {
                return string.Empty;
            }

            int maxSize = 0;
            foreach (string path in paths)
            {
                maxSize += path?.Length ?? 0;
            }
            maxSize += paths.Length - 1;

            var builder = new StringBuilder(260); // MaxShortPath on Windows
            builder.EnsureCapacity(maxSize);

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (builder.Length == 0)
                {
                    builder.Append(path);
                }
                else
                {
                    if (!host.IsDirectorySeparator(builder[^1]) && !host.IsDirectorySeparator(path[0]))
                    {
                        builder.Append(host.DirectorySeparatorChar);
                    }

                    builder.Append(path);
                }
            }

            return builder.ToString();
        }

        public static FileInfo Unwrap(this IFileInfo fileInfo)
        {
            return new FileInfo(fileInfo.FullName);
        }

        public static DirectoryInfo Unwrap(this IDirectoryInfo directoryInfo)
        {
            return new DirectoryInfo(directoryInfo.FullName);
        }

        /// <summary>
        /// True if the given character is a directory separator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsDirectorySeparator(this IPath host, char c)
        {
            return c == host.DirectorySeparatorChar || c == host.AltDirectorySeparatorChar;
        }
    }
}