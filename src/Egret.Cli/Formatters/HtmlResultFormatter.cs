using Egret.Cli.Processing;
using System.Threading.Tasks;

namespace Egret.Cli.Formatters
{
    public class HtmlResultFormatter : IResultFormatter
    {
        public ValueTask DisposeAsync()
        {
            throw new System.NotImplementedException();
        }

        public ValueTask WriteResult(int index, TestCaseResult result)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask WriteResultsFooter(int count, int successes, int failures)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask WriteResultsHeader()
        {
            throw new System.NotImplementedException();
        }
    }
}