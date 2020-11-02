using System;
using System.CommandLine.IO;
using System.CommandLine.Rendering;

namespace Egret.Cli.Extensions
{
    public static class TerminalExtensions
    {
        private static readonly TextSpan NewLine = new ContentSpan(Environment.NewLine);

        public static void WriteLine(this ITerminal terminal, string message)
        {
            terminal.Out.WriteLine(message);
        }

        public static void WriteLine(this ITerminal terminal, params TextSpan[] components)
        {
            terminal.Render(new ContainerSpan(components));
            terminal.Out.WriteLine();
        }

        public static ContainerSpan Rgb(this string @string, byte r, byte g, byte b)
        {

            return new ContainerSpan(
                ForegroundColorSpan.Rgb(r, g, b),
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }

        public static ContainerSpan Color(this string @string, ForegroundColorSpan color)
        {
            return new ContainerSpan(
                color,
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }
        public static ContainerSpan Highlight(this string @string, BackgroundColorSpan color)
        {
            return new ContainerSpan(
                color,
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }
        public static ContainerSpan Success(this string @string)
        {
            return new ContainerSpan(
                ForegroundColorSpan.LightGreen(),
                new ContentSpan(@string),
                ForegroundColorSpan.Reset());
        }
        public static ContainerSpan Bold(this string @string)
        {
            return new ContainerSpan(
                StyleSpan.BoldOn(),
                new ContentSpan(@string),
                StyleSpan.BoldOff());
        }
        public static ContainerSpan Underline(this string @string)
        {
            return new ContainerSpan(
                StyleSpan.UnderlinedOn(),
                new ContentSpan(@string),
                StyleSpan.UnderlinedOff());
        }
    }
}