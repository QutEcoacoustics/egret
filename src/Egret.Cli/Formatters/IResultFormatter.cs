using Egret.Cli.Processing;
using System;
using System.Threading.Tasks;
using Egret.Cli.Models.Results;

namespace Egret.Cli.Formatters
{
    public interface IResultFormatter : IAsyncDisposable
    {
        ValueTask WriteResult(int index, TestCaseResult result);
        ValueTask WriteResultsFooter(FinalResults finalResults);
        ValueTask WriteResultsHeader();
    }
}