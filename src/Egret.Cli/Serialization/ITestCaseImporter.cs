using Egret.Cli.Models;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Egret.Cli.Serialization
{
    /// <summary>
    /// A implemented of this class tests a string based specification for the importing of test cases.
    /// If the implemented <see cref="CanProcess"/> the specification, it will be selected to actually
    /// load the imported test cases.
    /// </summary>
    public interface ITestCaseImporter
    {
        public Validation<Error, Option<IEnumerable<string>>> CanProcess(string matcher, Config config);

        IAsyncEnumerable<TestCase> Load(IEnumerable<string> resolvedSpecifications, ImporterContext context);
    }

    public record ImporterContext(TestCaseInclude Include, Config Config, ConfigDeserializer Deserializer);
}