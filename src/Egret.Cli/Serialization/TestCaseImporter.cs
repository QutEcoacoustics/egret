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
        public async ValueTask<Seq<Error>> LoadImportedTestCases(Suite suite, Config config, ConfigDeserializer deserializer)
        {
            if (suite.IncludeTests.Count == 0)
            {
                return Empty;
            }

            Lst<TestCaseInclude> modifiedIncludes = Empty;
            for (int i = 0; i < suite.IncludeTests.Count; i++)
            {
                TestCaseInclude include = suite.IncludeTests[i];
                var providerResult = FindProviderOrThrow(include, config);

                if (providerResult.IsFail)
                {
                    return providerResult.FailToSeq();
                }

                var (provider, refinedFroms) = ((ITestCaseImporter, IEnumerable<string>))providerResult;

                // warning: recursive! each import could potentially have more imports!
                var tests = await provider.Load(refinedFroms, new ImporterContext(include, config, deserializer)).ToArrayAsync();

                logger.LogTrace("Provider {provider} did match {from}, returned {count} results", provider.GetType().Name, include.From, tests.Length);
                modifiedIncludes += include with { Tests = tests };
            }

            suite.IncludeTests = modifiedIncludes.ToArr();

            return Empty;
        }

        private Validation<Error, (ITestCaseImporter, IEnumerable<string>)> FindProviderOrThrow(TestCaseInclude include, Config config)
        {
            logger.LogTrace("Loading includes for {from}", include.From);

            Seq<Error> errors = Empty;
            foreach (var provider in Importers)
            {
                // can the provider process this include?
                var testCan = provider.CanProcess(include.From, config);

                if (testCan.IsFail)
                {
                    errors += testCan.FailToSeq();
                    continue;
                }

                if (testCan.IfFail(None).Case is IEnumerable<string> refinedFroms)
                {
                    return (provider, refinedFroms);
                }

                logger.LogTrace("Provider {provider} did not match {from}", provider, include.From);

                // try next provider
            }

            return Error.New($"Cannot load includes: {include.From} was not found or we don't know how to load tests from this type of file").Cons(errors);
        }
    }
}