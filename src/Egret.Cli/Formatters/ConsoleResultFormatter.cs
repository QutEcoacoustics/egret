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
using static Egret.Cli.Hosting.EgretConsole;

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

        public async ValueTask WriteResultsFooter(int count, int successes, int failures)
        {
            var resultSpan = (successes / (double)count).ToString("P").StyleNumber();
            var finalMessage = new ContainerSpan(
                NewLine,
                "Finished. Final results:".StyleUnderline(),
                NewLine,
                Tab,
                "Successes: ".AsTextSpan(),
                successes.ToString().StyleNumber(),
                NewLine,
                Tab,
                "Failures: ".AsTextSpan(),
                failures.ToString().StyleFailure(),
                NewLine,
                Tab,
                "Total: ".AsTextSpan(),
                resultSpan);
            console.WriteLine(finalMessage);

            await ValueTask.CompletedTask;
        }

        public ContainerSpan Format(int index, TestCaseResult result)
        {
            var formattedErrors = FormatErrors(result.Errors);
            var formattedEventResults = ResultSection("Events", result.Results.Where(e => e is { Subject: Expectation }));
            var formattedAggregateResults = ResultSection("Segment tests", result.Results.Where(e => e is { Subject: AggregateExpectation }));

            var performance = result.Context.ExecutionTime.TotalSeconds;

            return new ContainerSpan(
                FormatSuccess(result.Success),
                Space,
                result.Context.SuiteName.AsTextSpan(),
                result.Context.ExecutionIndex.ToString("\\.##0: ").AsTextSpan(),
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
            return subset.SelectMany(this.FormatExpectation).Prepend(header);
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
                literateSerializer.OneLine(result.Subject).TrimEnd().StyleUnimportant()
            );

            if (result.Successful)
            {
                yield break;
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

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}