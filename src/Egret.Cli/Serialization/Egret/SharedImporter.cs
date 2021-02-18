using Egret.Cli.Hosting;
using Egret.Cli.Models;
using Egret.Cli.Processing;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static LanguageExt.Prelude;

namespace Egret.Cli.Serialization
{
    /// <summary>
    /// Imports tests from the shared section of the config file.
    /// </summary>
    public class SharedImporter : ITestCaseImporter
    {
        private readonly ILogger<SharedImporter> logger;
        private readonly string SharedTestsKey;

        public SharedImporter(ILogger<SharedImporter> logger, INamingConvention configNaming)
        {
            this.logger = logger;
            SharedTestsKey = configNaming.Apply(nameof(Config.CommonTests));
        }

        public Validation<Error, Option<IEnumerable<string>>> CanProcess(string matcher, Config config)
        {
            if (matcher == SharedTestsKey)
            {
                return Some(config.CommonTests.Keys.AsEnumerable());
            }

            return Option<IEnumerable<string>>.None;
        }

        public IAsyncEnumerable<TestCase> Load(IEnumerable<string> resolvedSpecifications, ImporterContext context)
        {

            if (context.Include.SpectralTolerance is not null || context.Include.TemporalTolerance is not null)
            {
                logger.LogWarning(
                    $"{nameof(TestCaseInclude.SpectralTolerance)} and {nameof(TestCaseInclude.TemporalTolerance)} are ignored when importing tests from the `common_tests` section. You can put tolerances on the tests directly.");
            }

            // note to future me: there's no point outputting console context here
            // because the globs aren't expanded yet

            return Sync().ToAsyncEnumerable();

            IEnumerable<TestCase> Sync()
            {
                var matcher = context.Include.FilterMatcher;
                foreach (var key in resolvedSpecifications)
                {
                    if (!matcher.Match(key).HasMatches)
                    {
                        logger.LogDebug("Filtered out common tests for {key}", key);
                        continue;
                    }

                    var result = context.Config.CommonTests[key];
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
