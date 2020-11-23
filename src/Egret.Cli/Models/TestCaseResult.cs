using Egret.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Egret.Cli.Processing
{
    public partial record TestCaseResult(
        IReadOnlyList<string> Errors,
        IReadOnlyList<ExpectationResult> Results,
        TestContext Context);


    public partial record TestCaseResult
    {
        public bool Success =>
         this.Errors is { Count: 0 }
         && (Results?.All(x => x.Successful) ?? false);
    }


    public record TestContext(
        Suite Suite,
        string ToolName,
        string SourceName,
        TimeSpan ExecutionTime);
}