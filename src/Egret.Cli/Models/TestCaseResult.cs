using Egret.Cli.Models;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using static Egret.Cli.Processing.CaseExecutor;

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
        string SuiteName,
        string ToolName,
        string ToolVersion,
        string SourceName,
        int ExecutionIndex,
        CaseTracker CaseTracker,
        TimeSpan ExecutionTime);
}