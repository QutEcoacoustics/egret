using Microsoft.Extensions.FileSystemGlobbing;
using MoreLinq;
using System;
using System.CommandLine.Parsing;
using System.Diagnostics;

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
            if (multiGlobPattern is null or { Length: 0 })
            {
                throw new ArgumentException("multiGlobPattern must not be null or empty");
            }

            var result = new MultiGlob();

            var segments = multiGlobPattern.Split("|");
            foreach (var segment in segments)
            {
                switch (segment.StartsWith("!"), segment.Length)
                {
                    case (true, > 1):
                        result.AddExclude(segment[1..]);
                        break;
                    case (false, > 0):
                        result.AddInclude(segment);
                        break;
                    case (true, 1):
                        throw new ArgumentException($"Segment `{segment}` in multiglob `{multiGlobPattern} has only a `!` in it... pattern is too short, there is nothing to negate");
                    case (false, 0):
                        throw new ArgumentException($"There is an empty in multiglob `{multiGlobPattern} pattern is too short, there is nothing to match against");
                    default:
                        throw new InvalidOperationException($"Unknown glob segment `{segment}` from `{multiGlobPattern}`");
                }
            }

            return result;
        }

        private static readonly char[] GlobChars = new[] { '*', '|' };

        public static bool TestIfMultiGlob(string possibleGlob)
        {
            return possibleGlob.IndexOfAny(GlobChars) >= 0;
        }
    }
}