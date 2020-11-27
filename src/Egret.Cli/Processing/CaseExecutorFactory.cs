
using Egret.Cli.Hosting;
using Egret.Cli.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using static Egret.Cli.Processing.CaseExecutor;

namespace Egret.Cli.Processing
{
    public class CaseExecutorFactory
    {
        private readonly IServiceProvider provider;

        public CaseExecutorFactory(IServiceProvider provider)
        {
            this.provider = provider;
        }

        public CaseExecutor Create(TestCase @case, Tool tool, Suite suite, in CaseTracker tracker)
        {
            var instance = new CaseExecutor(
                provider.GetRequiredService<ILogger<CaseExecutor>>(),
                provider.GetRequiredService<EgretConsole>(),
                provider.GetRequiredService<ToolRunner>(),
                provider.GetRequiredService<TempFactory>(),
                provider.GetRequiredService<HttpClient>()
            )
            {
                Case = @case,
                Tool = tool,
                Suite = suite,
                Tracker = tracker,
            };
            return instance;
        }
    }
}