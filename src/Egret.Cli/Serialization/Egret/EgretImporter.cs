using Egret.Cli.Models;
using LanguageExt;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static LanguageExt.Prelude;

namespace Egret.Cli.Serialization.Egret
{
    /// <summary>
    /// Imports test suites from another egret configuration file
    /// </summary>
    public class EgretImporter : ITestCaseImporter
    {

        public static readonly IReadOnlyList<string> EgretConfigExtension = new[] {
            ".egret.yml",
            ".egret.yaml"
        };

        public static bool IsEgretConfigFile(string path)
        {
            Debug.Assert(EgretConfigExtension.Count == 2, "hardcased for 2 items");

            return path.EndsWith(EgretConfigExtension[0]) || path.EndsWith(EgretConfigExtension[1]);
        }

        public Option<IEnumerable<string>> CanProcess(Matcher matcher, Config config)
        {
            var result = matcher.GetResultsInFullPath(config.Location.DirectoryName);
            return result.Any() && result.All(IsEgretConfigFile) ? Some(result) : None;
        }



        public IAsyncEnumerable<TestCase> Load(IEnumerable<string> resolvedSpecifications, TestCaseInclude include, Config config, TestCaseImporter recursiveImporter)
        {
            throw new NotImplementedException();
            foreach (var path in resolvedSpecifications)
            {

            }
        }
    }
}