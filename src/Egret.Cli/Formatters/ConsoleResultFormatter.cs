using Egret.Cli.Extensions;
using Egret.Cli.Hosting;
using Egret.Cli.Models;
using Egret.Cli.Processing;
using Egret.Cli.Serialization;
using StringTokenFormatter;
using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Egret.Cli.Models.Results;
using System.Text;
using System.Linq.Expressions;
using LanguageExt;
using static Egret.Cli.Hosting.EgretConsole;
using static LanguageExt.Prelude;

namespace Egret.Cli.Formatters
{
    public class ConsoleResultFormatter : IResultFormatter
    {
        private readonly LiterateSerializer literateSerializer;
        private readonly EgretConsole console;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private static readonly Interval ExecutionTimeGrade = new Interval(10, 1);

        public ConsoleResultFormatter(LiterateSerializer literateSerializer, EgretConsole console)
        {
            this.literateSerializer = literateSerializer;
            this.console = console;
        }

        public async ValueTask WriteResultsHeader()
        {
            console.WriteLine("Results".StyleUnderline());

            await ValueTask.CompletedTask;
        }

        public async ValueTask WriteResult(int index, TestCaseResult result)
        {
            // restrict async execution to be synchronous while writing output
            await semaphore.WaitAsync();
            try
            {
                console.WriteLine(this.Format(index, result));
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async ValueTask WriteResultsFooter(FinalResults finalResults)
        {
            var stats = finalResults.ResultStatistics;
            var performance = stats.TotalSuccesses / (double)stats.TotalResults;
            var resultSpan = performance.ToString("P").StyleGrade(performance, new Interval(0, 100, Topology.Inclusive));
            var finalMessage = new ContainerSpan(
                NewLine,
                "Finished.".StyleUnderline(),
                NewLine,
                "Final results:".AsTextSpan(),
                NewLine,
                FormatStat("Successes", stats.TotalSuccesses.ToString().StyleSuccess()),
                FormatStat("Failures", stats.TotalFailures.ToString().StyleFailure()),
                FormatStat("Errors", stats.TotalErrors.ToString().StyleFailure()),
                FormatStat("Total", resultSpan),
                FormatStat("Elapsed", finalResults.TimeTaken.ToString().StyleNumber())
            );
            console.WriteLine(finalMessage);

            foreach (var (suiteName, suite) in finalResults.Config.TestSuites)
            {
                console.WriteLine($"Results for suite {suiteName.StyleBold()}:".StyleUnderline());

                foreach (var (toolName, _) in suite.ToolConfigs)
                {
                    var processingErrors = stats.ProcessingErrors.Find(suiteName, toolName);
                    if (processingErrors.Case is int i && i > 0)
                    {
                        console.WriteLine(new ContainerSpan(
                            i.ToString().StyleFailure(),
                            " errors were encountered while running ".AsTextSpan(),
                            toolName.StyleValue(),
                            NewLine
                        ));
                    }

                    foreach (var type in ResultsStatistics.Keys)
                    {
                        var summary = stats.Stats.Find(suiteName, toolName, type);

                        console.WriteLine(summary.Case switch
                        {
                            BinaryStatistics b => FormatStatistics($"Summary for {suiteName}, {toolName}, {type}-level", type, b),
                            _ => $"No summary statistics found for  {suiteName}, {toolName}, {type}-level".AsTextSpan()
                        });
                    }
                }
            }

            // if we have more than one suite - pointless otherwise
            if (stats.Stats.Count > 1)
            {
                console.WriteLine($"Grand total results:".StyleUnderline());
                var eventStatistics = FormatStatistics("Grand total event-level", "Events", finalResults.ResultStatistics.GrandTotalEvents);
                console.WriteLine(eventStatistics);

                var segmentStatistics = FormatStatistics("Grand total segment-level", "segments", finalResults.ResultStatistics.GrandTotalSegments);
                console.WriteLine(segmentStatistics);
            }

            await ValueTask.CompletedTask;

            TextSpan FormatStat(string name, TextSpan value) => new ContainerSpan((name + ": ").AsTextSpan(), SoftTab, value, SoftTab);
        }

        public ContainerSpan Format(int index, TestCaseResult result)
        {
            var formattedErrors = FormatErrors(result.Errors);
            var formattedEventResults = ResultSection("Events", result.Results.Where(e => e is { Subject: IEventExpectation }));
            var formattedAggregateResults = ResultSection("Segment tests", result.Results.Where(e => e is { Subject: ISegmentExpectation }));

            var performance = result.Context.ExecutionTime.TotalSeconds;

            return new ContainerSpan(
                FormatSuccess(result.Success),
                Space,
                result.Context.SuiteName.AsTextSpan(),
                $"#{result.Context.ExecutionIndex:##0}: ".AsTextSpan(),
                result.Context.TestName is { Length: > 0 } ? $"[{result.Context.TestName}]".AsTextSpan() : TextSpan.Empty(),
                performance.ToString("{0.00 s} ").StyleGrade(performance, ExecutionTimeGrade),
                "for ".AsTextSpan(),
                result.Context.ToolName.StyleValue(),
                result.Context.ToolVersion switch
                {
                    string s => new ContainerSpan(" (".AsTextSpan(), s.StyleNumber(), ")".AsTextSpan()),
                    null => TextSpan.Empty()
                },
                " with ".AsTextSpan(),
                result.Context.SourceName.StyleValue(),
                new ContainerSpan(formattedErrors.Concat(formattedEventResults).Concat(formattedAggregateResults).ToArray())
            );
        }

        private IEnumerable<TextSpan> FormatErrors(IReadOnlyList<string> errors)
        {
            foreach (var error in errors)
            {
                yield return new ContainerSpan(
                    NewLine,
                    SoftTab,
                    " - ".AsTextSpan(),
                    error.StyleFailure()
                );
            }
        }

        private IEnumerable<TextSpan> ResultSection(string name, IEnumerable<ExpectationResult> subset)
        {
            if (!subset.Any())
            {
                return Enumerable.Empty<TextSpan>();
            }

            var header = new ContainerSpan(
                            NewLine,
                            SoftTab,
                            $"{name}: ".AsTextSpan()
                        );
            return subset.SelectMany(FormatExpectation).Prepend(header);
        }

        private IEnumerable<TextSpan> FormatExpectation(ExpectationResult result, int index)
        {
            yield return new ContainerSpan(
                NewLine,
                SoftTab2,
                $"- ".AsTextSpan(),
                FormatSuccess(result.Successful),
                $" {index}: ".AsTextSpan(),
                (result.Subject.Name is null ? string.Empty : result.Subject.Name + " ").AsTextSpan(),
                result.Successful ? TextSpan.Empty() : literateSerializer.OneLine(result.Subject).TrimEnd().StyleUnimportant()
            );

            if (result.Successful)
            {
                yield break;
            }

            if (result.Target is not null)
            {
                yield return new ContainerSpan(
                    NewLine,
                    SoftTab3,
                    " - ℹ Matched result: ".AsTextSpan(),
                    result.Target.ToString().StyleUnimportant()
                );
            }

            foreach (var assertion in result.Assertions)
            {
                yield return new ContainerSpan(
                    NewLine,
                    SoftTab3,
                    "- ".AsTextSpan(),
                    FormatSuccess(assertion is SuccessfulAssertion),
                    Space,
                    assertion.Name.AsTextSpan(),
                    assertion.MatchedKey is null
                        ? TextSpan.Empty()
                        : new ContainerSpan(" matches ".AsTextSpan(), assertion.MatchedKey.StyleValue())
                );

                yield return assertion switch
                {
                    FailedAssertion f => new ContainerSpan(
                        ": ".AsTextSpan(),
                        f.Reasons.JoinWithComma().StyleFailure()
                    ),
                    ErrorAssertion e => new ContainerSpan(
                        ": ".AsTextSpan(),
                        "Error".StyleHighlight(BackgroundColorSpan.Red()),
                        Space,
                        e.Reasons.JoinWithComma().StyleFailure()
                    ),
                    SuccessfulAssertion _ => TextSpan.Empty(),
                    _ => throw new InvalidOperationException(),
                };
            }
        }

        public static TextSpan FormatStatistics(string title, string name, BinaryStatistics s)
        {
            if (s is null)
            {
                return "No statistics available".AsTextSpan();
            }

            // this layout is designed to be as similar to the following wikipedia article as possible:
            // https://en.wikipedia.org/wiki/Evaluation_of_binary_classifiers
            // in particular the layout follows the same form as the contingency table
            string Empty = string.Empty;

            var errorsTitle = s.Errors > 0 ? "Errors" : Empty;
            var errorsValue = s.Errors > 0 ? V(s.Errors) : Empty;

            var items = new string[,] {
                { Empty,            Empty,                  "Labelled +ve",  Empty,                   "Labelled -ve",   Empty,                   Empty,              Empty,                  Empty,       Empty},
                { $"Total {name}:", V(s.TotalConditions),    Empty,          V(s.ConditionPositives),  Empty,           V(s.ConditionNegatives), "Prevalance:",      P(s.Prevalance),        "Accuracy:", P(s.Accuracy)},
                { "Results +ve:",   V(s.PredictedPositives), "TP:",          V(s.TruePositives),       "FP:",           V(s.FalsePositives),     "Precision (PPV):", P(s.Precision),         "FDR:",      P(s.FalseDiscoveryRate)},
                { "Results -ve:",   V(s.PredictedNegatives), "FN:",          V(s.FalseNegatives),      "TN:" ,          V(s.TrueNegatives),      "FOR:",             P(s.FalseOmissionRate), "NPV:",      P(s.NegativePredictiveValue)},
                { "Results Count:", V(s.ConditionPositives), "Sensitivity:", P(s.Sensitivity),         "FPR:",          P(s.FalsePositiveRate),  Empty,              Empty,                  Empty,       Empty},
                { Empty,            Empty,                   "FNR:",         P(s.FalseNegativeRate),   "Specificity:",  P(s.Specificity),        Empty,              Empty,                  errorsTitle, errorsValue},
            };

            // get widest width for each column
            int width = items.GetUpperBound(1) + 1;
            int[] colWidths = new int[width];
            int height = items.GetUpperBound(0) + 1;
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    colWidths[c] = Math.Max(colWidths[c], items[r, c].Count(c => c < 255));
                }
            }

            // finally build the string and print it all out
            ContainerSpan[] rows = new ContainerSpan[height];
            for (int r = 0; r < height; r++)
            {
                var columns = Seq<TextSpan>();
                for (int c = 0; c < width; c++)
                {
                    // we need to apply ansi coloring after we calculate column widths
                    columns += FormatPosition(r, c, colWidths[c], items[r, c]);
                }
                columns = columns.Add(NewLine);

                rows[r] = new ContainerSpan(columns.ToArray());
            }

            return new ContainerSpan(
                (title + ":").AsTextSpan(),
                NewLine,
                new ContainerSpan(rows)
            );

            // value, good, bad, percentage (respectively)
            string V(double value) => $"{value:N0}";
            string P(double value) => $"{value:P}";

            Seq<TextSpan> FormatPosition(int i, int j, int width, string input)
            {
                TextSpan token = (i, j) switch
                {
                    (1 or 2 or 3 or 4, 1) => input.PadLeft(width).StyleValue(),
                    (1 or 4 or 5, 3 or 5) => input.PadLeft(width).StyleValue(),
                    (2, 3) => input.PadLeft(width).StyleSuccess(),
                    (3, 3) => input.PadLeft(width).StyleFailure(),
                    (2, 5) => input.PadLeft(width).StyleFailure(),
                    (3, 5) => input.PadLeft(width).StyleSuccess(),
                    (1 or 2 or 3, 7 or 9) => input.PadLeft(width).StyleValue(),
                    (5, 8) when input.Length > 0 => input.PadRight(width).StyleFailure(),
                    (5, 9) when input.Length > 0 => input.PadLeft(width).StyleFailure(),
                    _ when input is { Length: 0 } => new string(' ', width).AsTextSpan(),
                    _ => input.PadRight(width).AsTextSpan()
                };
                return Seq(token, Space);
            }

        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}