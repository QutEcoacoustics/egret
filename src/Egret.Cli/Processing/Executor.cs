
using Egret.Cli.Extensions;
using Egret.Cli.Hosting;
using Egret.Cli.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using static Egret.Cli.Processing.CaseExecutor;

namespace Egret.Cli.Processing
{

    public class Executor
    {
        private readonly EgretConsole console;

        private readonly ILogger<Executor> logger;
        private readonly CaseExecutorFactory factory;

        public Executor(ILogger<Executor> logger, CaseExecutorFactory factory, EgretConsole console)
        {
            this.logger = logger;
            this.factory = factory;
            this.console = console;
        }



        public async Task<List<SuiteResult>> RunSuiteAsync(Config config, bool parallel = false)
        {
            logger.LogTrace("Generating tasks");
            var cases = GenerateTasks(config).ToArray();

            console.WriteRichLine($"Found {cases.Length} tests, running tests:");

            int total = cases.Length;
            int progress = 0;

            var results = new List<SuiteResult>(cases.Length);
            await foreach (var result in cases.ForEachAsync<CaseExecutor, SuiteResult>())
            {
                var percentage = Interlocked.Increment(ref progress) / (double)total;
                logger.LogDebug("Progress: {progress:P}", percentage);



                results.Add(result);
            }

            logger.LogTrace("Results collected: {count}", progress);

            return results;
        }

        public IEnumerable<CaseExecutor> GenerateTasks(Config config)
        {
            ushort suiteIndex = 0;
            foreach (var (suiteName, suite) in config.TestSuites)
            {

                ushort toolIndex = 0;

                foreach (var (toolName, tool) in config.Tools)
                {
                    ushort caseIndex = 0;


                    foreach (var @case in suite.Tests)
                    {
                        yield return factory.Create(@case, tool, suite, new CaseTracker(caseIndex, toolIndex, suiteIndex));
                        caseIndex++;
                    }

                    foreach (var (name, caseSet) in suite.SharedCases)
                    {
                        foreach (var @case in caseSet)
                        {
                            yield return factory.Create(@case, tool, suite, new CaseTracker(caseIndex, toolIndex, suiteIndex));
                            caseIndex++;
                        }
                    }

                    toolIndex++;
                }

                suiteIndex++;
            }
        }
    }
}