using Egret.Cli.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.IO;

namespace Egret.Cli.Hosting
{
    public class EgretConsole
    {
        public static readonly TextSpan NewLine = new ContentSpan(Environment.NewLine);
        public static readonly TextSpan Space = new ContentSpan(" ");
        public static readonly TextSpan Tab = new ContentSpan("\t");
        public static readonly TextSpan SoftTab = new ContentSpan("  ");
        public static readonly TextSpan SoftTab2 = new ContentSpan("    ");
        public static readonly TextSpan SoftTab3 = new ContentSpan("      ");
        public static readonly TextSpan SoftTab4 = new ContentSpan("        ");


        private readonly IConsole console;
        private readonly ITerminal terminal;
        private readonly OutputMode outputMode;
        private readonly TextSpanFormatter formatter;
        private readonly ILogger<EgretConsole> logger;


        private static readonly TextSpan success = new ContentSpan("\u2705"); // "SUCCESS".Success()
        private static readonly TextSpan failure = new ContentSpan("\u274C"); //  "FAILED ".Failure()

        public EgretConsole(IConsole console, ILogger<EgretConsole> logger)
        {
            this.logger = logger;

            this.console = console;

            this.terminal = console.GetTerminal(preferVirtualTerminal: true, OutputMode.Auto);
            this.outputMode = (terminal ?? console).DetectOutputMode();
            this.formatter = new TextSpanFormatter();

            this.formatter.AddFormatter<FileInfo>((f) => f.FullName.StyleValue());
            this.formatter.AddFormatter<double>((x) => x.ToString("N2").StyleNumber());
            this.formatter.AddFormatter<int>((x) => x.ToString().StyleNumber());
        }
        public static TextSpan FormatSuccess(bool test) => test ? success : failure;

        public void WriteLine(string message)
        {
            //this.terminal.Render( , Region.Scrolling);
            //terminal.Render(new ContentSpan(message));
            //terminal.Out.WriteLine();
            //terminal.Append(new ContentSpan(message));
            (terminal ?? console).Out.WriteLine(message);
            logger.LogInformation(message);
        }

        public void WriteLine(params TextSpan[] components)
        {
            //terminal.Append(new View());
            //var t = terminal.GetTerminal(true);

            //terminal.Render(new ContainerSpan(components).ToString());
            var text = new ContainerSpan(components).ToString(outputMode);
            (terminal ?? console).Out.WriteLine(text);
            logger.LogInformation(text);


            //terminal.Out.WriteLine(terminal.)
            //terminal.Out.WriteLine();
            //terminal.Append(new ContainerSpan(components));
            //terminal.Append(NewLine);

        }

        public void WriteRichLine(FormattableString message)
        {
            this.WriteLine(this.formatter.ParseToSpan(message));
        }


    }
}