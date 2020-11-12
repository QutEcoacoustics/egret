using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.ComponentModel;

namespace Egret.Cli.Commands
{
    public class MainCommand
    {

        public static readonly ICommandHandler RunHandler = CommandHandler.Create<IHost, InvocationContext, IHelpBuilder>(Run);
        public static int Run(IHost host, InvocationContext context, IHelpBuilder help)
        {
            var log = host.Services.GetRequiredService<ILogger<Program>>();
            log.LogDebug("Run main");

            help.Write(context.ParseResult.CommandResult.Command);

            return 0;
        }
    }
}