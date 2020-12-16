using Egret.Cli.Models;
using Egret.Cli.Processing;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Egret.Cli.Serialization
{
    public class TestCaseImporter
    {
        private readonly ILogger<TestCaseImporter> logger;
        public TestCaseImporter(ILogger<TestCaseImporter> logger, ITestCaseImporter[] importers)
        {
            this.logger = logger;

            Importers = importers;
        }

        public ITestCaseImporter[] Importers { get; }

        /// <summary>
        /// Loads test cases from another source
        /// </summary>
        /// <param name="config">The rest of the config that was loaded.</param>
        /// <returns>Loaded test cases</returns>
        public async ValueTask<Seq<Error>> LoadImportedTestCases(TestCaseSet testCaseSet, Config config)
        {
            if (testCaseSet.IncludeTests.Length == 0)
            {
                return Empty;
            }

            for (int i = 0; i < testCaseSet.IncludeTests.Length; i++)
            {
                TestCaseInclude include = testCaseSet.IncludeTests[i];
                var providerResult = FindProviderOrThrow(include, config);

                if (providerResult.Case is Seq<string> failure)
                {
                    return failure.Map(Error.New);
                }
                var (provider, refinedFroms) = ((ITestCaseImporter, IEnumerable<string>))providerResult;

                // warning: recursive! each import could potentially have more imports!
                var tests = await provider.Load(refinedFroms, include, config, recursiveImporter: this).ToArrayAsync();

                logger.LogTrace("Provider {provider} did match {from}, returned {count} results", provider.GetType().Name, include.From, tests.Length);
                testCaseSet.IncludeTests[i] = include with { Tests = tests };
            }

            return Empty;
        }

        private Validation<string, (ITestCaseImporter, IEnumerable<string>)> FindProviderOrThrow(TestCaseInclude include, Config config)
        {
            logger.LogTrace("Loading includes for {from}", include.From);

            // convert from to glob
            var matcher = MultiGlob.Parse(include.From);
            foreach (var provider in Importers)
            {
                // can the provider process this include?
                var can = provider.CanProcess(matcher, config);

                if (can.Case is IEnumerable<string> refinedFroms)
                {
                    return (provider, refinedFroms);
                }

                logger.LogTrace("Provider {provider} did not match {from}", provider, include.From);

                // try next provider
            }

            return $"Cannot load includes: {include.From} was not found or we don't know how to load tests from this type of file";
        }
    }
}