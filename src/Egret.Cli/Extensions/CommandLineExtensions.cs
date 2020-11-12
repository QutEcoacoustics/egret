using Egret.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Egret.Cli.Extensions
{
    public static class CommandLineExtensions
    {
        public static Option<T> WithAlias<T>(this Option<T> option, string alias)
        {
            option.AddAlias(alias);
            return option;
        }

        public static IHostBuilder UseEgretCommand<TOptions, TCommand>(this IHostBuilder builder, string commandName)
        where TOptions : class, new()
        where TCommand : class, IEgretCommand
        {
            if (builder.Properties[typeof(InvocationContext)] is not InvocationContext invocation)
            {
                throw new InvalidOperationException("InvocationContext is not available");
            }

            var command = (Command)invocation.ParseResult.CommandResult.Command;

            if (command.Name != commandName)
            {
                return builder;
            }

            command.Handler = CommandHandler.Create<IHost, InvocationContext>(async (host, context) =>
            {
                using (host.Services.GetService<ILogger<TCommand>>().Measure(commandName))
                {
                    var ourCommand = host.Services.GetRequiredService<TCommand>();
                    return await ourCommand.InvokeAsync(context);
                }
            });

            return builder.ConfigureServices((collection) =>
            {
                BindOptions<TOptions>(collection);
                collection.AddTransient<TCommand>();
            });


        }
        public static IHostBuilder UseEgretGlobalOptions<TOptions>(this IHostBuilder builder)
        where TOptions : class, new()
        {
            return builder.ConfigureServices(BindOptions<TOptions>);
        }


        private static void BindOptions<TOptions>(IServiceCollection collection)
                where TOptions : class, new()

        {
            collection.AddSingleton<ModelBinder<TOptions>>();
            collection.AddTransient<TOptions>((provider) =>
            {
                var context = provider.GetRequiredService<BindingContext>();
                var modelBinder = provider.GetRequiredService<ModelBinder<TOptions>>();
                var options = new TOptions();
                modelBinder.UpdateInstance(options, context);
                return options;
            });

        }
    }
}