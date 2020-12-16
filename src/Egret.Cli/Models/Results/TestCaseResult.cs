using Egret.Cli.Models;
using LanguageExt;
using LanguageExt.ClassInstances;
using Microsoft.Extensions.FileSystemGlobbing;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Egret.Cli.Processing.CaseExecutor;

namespace Egret.Cli.Models.Results
{
    //public record SuiteResult(string SuiteName)
    public partial record TestCaseResult(
        IReadOnlyList<string> Errors,
        IReadOnlyList<ExpectationResult> Results,
        TestContext Context);


    public partial record TestCaseResult
    {
        public bool Success =>
         Errors is { Count: 0 }
         && (Results?.All(x => x.Successful) ?? false);


    }


    public record TestContext(
        string SuiteName,
        string TestName,
        string ToolName,
        string ToolVersion,
        string SourceName,
        int ExecutionIndex,
        CaseTracker CaseTracker,
        TimeSpan ExecutionTime);


}