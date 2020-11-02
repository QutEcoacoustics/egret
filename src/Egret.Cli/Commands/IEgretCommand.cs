using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Egret.Cli.Commands
{
    public interface IEgretCommand
    {
        Task<int> InvokeAsync(InvocationContext context);
    }
}
