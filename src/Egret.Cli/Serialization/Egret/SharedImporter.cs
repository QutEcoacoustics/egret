using Egret.Cli.Hosting;
using Egret.Cli.Models;
using LanguageExt;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Egret.Cli.Serialization
{
    /// <summary>
    /// Imports tests from the shared section of the config file.
    /// </summary>
    public class SharedImporter : ITestCaseImporter
    {
        private readonly ILogger<SharedImporter> logger;
        private readonly EgretConsole console;

        public SharedImporter(ILogger<SharedImporter> logger, EgretConsole console)
        {
            this.logger = logger;
            this.console = console;
        }

        public Option<IEnumerable<string>> CanProcess(Matcher matcher, Config config)
        {
            if (config?.CommonTests?.Count is null or 0)
            {
                return None;
            }

            var result = matcher.Match(config.CommonTests.Keys);

            return result.HasMatches ? Some(result.Files.Select(fpm => fpm.Path)) : None;
        }

        public IAsyncEnumerable<TestCase> Load(IEnumerable<string> resolvedSpecifications, TestCaseInclude include, Config config, TestCaseImporter recursiveImporter)
        {
            if (include.SpectralTolerance is not null || include.TemporalTolerance is not null)
            {
                logger.LogWarning(
                    $"{nameof(TestCaseInclude.SpectralTolerance)} and {nameof(TestCaseInclude.TemporalTolerance)} are ignored when importing test from the `common_tests` section. You can put tolerances on the tests directly.");
            }

            // note to future me: there's no point outputting console context here
            // because the globs aren't expanded yet

            return Sync().ToAsyncEnumerable();

            IEnumerable<TestCase> Sync()
            {
                foreach (var key in resolvedSpecifications)
                {
                    var result = config.CommonTests[key];
                    foreach (var test in result)
                    {
                        yield return test;
                    }

                    logger.LogDebug("Loaded common tests for {key}", key);
                }
            }
        }
    }
}
