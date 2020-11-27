using Egret.Cli.Processing;
using System;
using System.Threading.Tasks;

namespace Egret.Cli.Formatters
{
    public interface IResultFormatter : IAsyncDisposable
    {
        ValueTask WriteResult(int index, TestCaseResult result);
        ValueTask WriteResultsFooter(int count, int successes, int failures);
        ValueTask WriteResultsHeader();
    }
}