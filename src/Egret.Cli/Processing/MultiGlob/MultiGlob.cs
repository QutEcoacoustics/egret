using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;

namespace Egret.Cli.Processing
{
    /// <summary>
    ///  we define our own mini syntax here so we can support negative includes
    /// <code>
    /// multi_glob = extended_glob [ "|" extended_glob ]*
    /// extended_glob = ["!"] glob
    /// glob = standard_blob 
    /// </code>
    /// For standard glob syntax see docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.filesystemglobbing.matcher?view=dotnet-plat-ext-5.0
    /// <summary>
    public class MultiGlob : Matcher
    {

        public static MultiGlob Parse(string multiGlobPattern)
        {
            return new MultiGlob(multiGlobPattern);
        }

        private static readonly char[] GlobChars = new[] { '!', '*', '|' };

        public string OriginalPattern { get; }

        public MultiGlob(string multiGlobPattern)
        {
            if (multiGlobPattern is null or { Length: 0 })
            {
                throw new ArgumentException("multiGlobPattern must not be null or empty");
            }

            var segments = multiGlobPattern.Split("|");
            int includes = 0, excludes = 0;
            foreach (var segment in segments)
            {
                switch (segment.StartsWith("!"), segment.Length)
                {
                    case (true, > 1):
                        AddExclude(Unescape(segment[1..]));
                        excludes++;
                        break;
                    case (false, > 0):
                        AddInclude(Unescape(segment));
                        includes++;
                        break;
                    case (true, 1):
                        throw new ArgumentException($"Segment `{segment}` in multiglob `{multiGlobPattern}` has only a `!` in it... pattern is too short, there is nothing to negate");
                    case (false, 0):
                        throw new ArgumentException($"There is an empty segment in multiglob `{multiGlobPattern}` pattern is too short, there is nothing to match against");
                    default:
                        throw new InvalidOperationException($"Unknown glob Segment `{segment}` from `{multiGlobPattern}`");
                }
            }

            if (excludes >= 1 && includes == 0)
            {
                AddInclude("**");
            }

            OriginalPattern = multiGlobPattern;

            // support basic escaping of bang characters
            static string Unescape(string fragment) => fragment.Replace("[!]", "!");
        }

        public static bool TestIfMultiGlob(string possibleGlob)
        {
            return possibleGlob is not null && possibleGlob.IndexOfAny(GlobChars) >= 0;
        }


    }

    public static class MatcherExtensions
    {
        /// <summary>
        /// Searches the directory specified for all files matching patterns added to this instance of <see cref="Matcher" />
        /// </summary>
        /// <param name="matcher">The matcher</param>
        /// <param name="directoryPath">The root directory for the search</param>
        /// <returns>Absolute file paths of all files matched. Empty enumerable if no files matched given patterns.</returns>
        /// <remarks>
        /// This is a direct rip off of https://github.com/dotnet/runtime/blob/1d9e50cb4735df46d3de0cee5791e97295eaf588/src/libraries/Microsoft.Extensions.FileSystemGlobbing/src/MatcherExtensions.cs#L52-L58
        /// adapted to work with an IFileSystem.
        /// </remarks>
        public static IEnumerable<string> GetResultsInFullPath(this Matcher matcher, IFileSystem fileSystem, string directoryPath)
        {
            var directory = fileSystem.DirectoryInfo.FromDirectoryName(directoryPath);
            var wrapper = new DirectoryInfoBaseAbstractionAdapter(directory);
            IEnumerable<FilePatternMatch> matches = matcher.Execute(wrapper).Files;
            string[] result = matches.Select(match => fileSystem.Path.GetFullPath(fileSystem.Path.Combine(directoryPath, match.Path))).ToArray();

            return result;
        }
    }
}