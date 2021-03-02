using Egret.Cli.Extensions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Egret.Cli.Hosting
{

    public class EgretConsole : IAsyncDisposable
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
        private readonly TextSpanFormatter formatter;
        private readonly ConsoleRenderer renderer;
        private readonly ILogger<EgretConsole> logger;


        private static readonly TextSpan success = new ContentSpan("\u2705"); // "SUCCESS".Success()
        private static readonly TextSpan failure = new ContentSpan("\u274C"); //  "FAILED ".Failure()
        private readonly AsyncContextThread context;
        private ProgressBar progress = null;

        public EgretConsole(IConsole providedConsole, ILogger<EgretConsole> providedLogger)
        {
            logger = providedLogger;

            console = providedConsole;

            terminal = console.GetTerminal(preferVirtualTerminal: true, OutputMode.Auto);
            OutputMode = (terminal ?? console).DetectOutputMode();
            formatter = new TextSpanFormatter();
            renderer = new ConsoleRenderer(terminal ?? console, resetAfterRender: false);

            formatter.AddFormatter<FileInfo>((f) => f.FullName.StyleValue());
            formatter.AddFormatter<double>((x) => x.ToString("N2").StyleNumber());
            formatter.AddFormatter<int>((x) => x.ToString().StyleNumber());

            var queue = new BlockingCollection<string>();

            // created our own little async event loop so we can run things sequentially
            context = new AsyncContextThread();
        }

        public OutputMode OutputMode { get; }

        public static TextSpan FormatSuccess(bool test) => test ? success : failure;

        public void WriteLine(string message)
        {
            WriteLine(new ContentSpan(message));
        }

        public void WriteLine(params TextSpan[] components)
        {
            WriteLine(new ContainerSpan(components));
        }

        public void WriteLine(TextSpan textSpan)
        {
            context.Factory.Run(() =>
            {
                var text = textSpan.ToString(OutputMode);
                if (progress is null)
                {
                    (terminal ?? console).Out.WriteLine(text);
                }
                else
                {
                    renderer.RenderToRegion(text, LastLine);
                    console.Out.Write($"{Ansi.Cursor.Move.NextLine(1)}");
                    RenderProgress(LastLine);
                }
                logger.LogInformation(text);
            });

        }

        public void WriteRichLine(FormattableString message)
        {
            WriteLine(formatter.ParseToSpan(message));
        }

        private Region LastLine => new Region(0, (terminal?.CursorTop ?? int.MaxValue) - 1);



        public async ValueTask CreateProgressBar(string title)
        {
            await context.Factory.Run(() =>
            {
                console.Out.Write($"{CursorControlSpan.Hide()}{Ansi.Cursor.Move.NextLine(1)}");
                progress = new ProgressBar() { Progress = 0, Title = title };
                RenderProgress();
            });

        }

        public void ReportProgress(double value)
        {
            context.Factory.Run(() =>
            {
                lock (context)
                {
                    if (progress != null)
                    {
                        progress.Progress = value;

                    }
                }
                RenderProgress();
            });

        }

        private void RenderProgress(Region r = null)
        {
            progress?.Render(renderer, r ?? LastLine);
        }

        public async ValueTask DestroyProgressBar()
        {
            await context.Factory.Run(() =>
            {
                ReportProgress(1.0);
                progress = null;

                WriteLine(new ContainerSpan(
                    CursorControlSpan.Show(),
                    ForegroundColorSpan.Reset(),
                    BackgroundColorSpan.Reset()));
            });
        }

        public async ValueTask DisposeAsync()
        {
            await context.JoinAsync();
        }

        private class ProgressBar : ContentView
        {
            public string Title { get; set; }
            public double Progress { get; set; }

            public override Size Measure(ConsoleRenderer renderer, Size maxSize)
            {
                return base.Measure(renderer, maxSize);
            }

            public override void Render(ConsoleRenderer renderer, Region region)
            {
                var p = Math.Clamp(Progress, 0, 1.0);
                var done = (int)(p * 100);

                const int textWidth = 6;
                var text = p.ToString("P");
                const int aHalf = 47;
                var firstFill = Math.Clamp(done, 0, aHalf);
                var secondFill = Math.Clamp(done - (aHalf + textWidth), 0, aHalf);

                var result = new ContainerSpan(
                    Title.StyleUnderline(),
                    ": ".AsTextSpan(),
                    new ContainerSpan(
                        new string('=', firstFill).StyleColor(ForegroundColorSpan.LightGreen()),
                        new string('-', aHalf - firstFill).StyleColor(ForegroundColorSpan.LightGray())
                    ),
                    done switch
                    {
                        <= aHalf => text.AsTextSpan(),

                        >= aHalf + textWidth => text.StyleSuccess(),

                        int m => new ContainerSpan(text[0..(m - aHalf)].StyleSuccess(), text[(m - aHalf)..^1].AsTextSpan()),
                    },
                    new ContainerSpan(
                        new string('=', secondFill).StyleColor(ForegroundColorSpan.LightGreen()),
                        new string('-', aHalf - secondFill).StyleColor(ForegroundColorSpan.LightGray())
                    )
                );
                base.Span = result;
                base.Render(renderer, region);
            }
        }
    }
}