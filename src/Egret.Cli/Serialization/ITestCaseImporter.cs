using Egret.Cli.Models;
using LanguageExt;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Egret.Cli.Serialization
{
    public interface ITestCaseImporter
    {
        public Option<IEnumerable<string>> CanProcess(Matcher matcher, Config config);

        IAsyncEnumerable<TestCase> Load(IEnumerable<string> resolvedSpecifications, TestCaseInclude include, Config config, TestCaseImporter recursiveImporter);

    }
}