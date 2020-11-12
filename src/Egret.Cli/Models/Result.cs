using Egret.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Egret.Cli.Processing
{
    public partial record SuiteResult(
        IReadOnlyList<string> Errors,
        IReadOnlyList<ExpectationResult> Results,
        SuiteResultContext Context);


    public partial record SuiteResult
    {
        public bool Success =>
         this.Errors is { Count: 0 }
         && (Results?.All(x => x.Successful) ?? false);
    }


    public record SuiteResultContext(
        Suite Suite,
        string ToolName,
        string SourceName,
        TimeSpan ExecutionTime);
}