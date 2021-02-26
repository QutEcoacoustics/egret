
using Egret.Cli.Extensions;
using Egret.Cli.Hosting;
using Egret.Cli.Models;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using static Egret.Cli.Processing.CaseExecutor;
using Egret.Cli.Models.Results;
using Microsoft.Extensions.FileSystemGlobbing;
using System.IO;

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

        public async Task<List<TestCaseResult>> RunAllSuitesAsync(Config config, IProgress<double> progressReporter, int maxParallelism)
        {
            logger.LogTrace("Generating tasks");
            var cases = GenerateTasks(config).ToArray();

            console.WriteRichLine($"Found {cases.Length} tests, running tests:");

            int total = cases.Length;
            int progress = 0;

            var results = await cases
                    .ForEachAsync<CaseExecutor, TestCaseResult>(
                        completed: UpdateProgress,
                        degreesOfParallelization: maxParallelism);

            logger.LogTrace("Results collected: {count}", progress);

            return results.ToList();

            void UpdateProgress(int index, TestCaseResult result)
            {
                var percentage = Interlocked.Increment(ref progress) / (double)total;
                progressReporter.Report(percentage);
                logger.LogDebug("Progress: {progress:P}", percentage);
            }
        }

        public IEnumerable<CaseExecutor> GenerateTasks(Config config)
        {
            ushort suiteIndex = 0;
            foreach (var (suiteName, suite) in config.TestSuites)
            {
                console.WriteRichLine($"Generating cases for {suiteName}...");

                ushort toolIndex = 0;

                foreach (var (toolName, tool) in config.Tools)
                {
                    ushort caseIndex = 0;

                    foreach (var @case in suite.GetAllTests())
                    {
                        yield return factory.Create(@case, tool, suite, config, new CaseTracker(caseIndex, toolIndex, suiteIndex));
                        caseIndex++;
                    }

                    toolIndex++;
                }

                suiteIndex++;
            }
        }



    }
}
