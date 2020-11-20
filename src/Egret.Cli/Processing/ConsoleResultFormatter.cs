using Egret.Cli.Extensions;
using Egret.Cli.Hosting;
using Egret.Cli.Models;
using Egret.Cli.Serialization;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.Linq;

using static Egret.Cli.Hosting.EgretConsole;

namespace Egret.Cli.Processing
{
    public class ConsoleResultFormatter
    {
        private readonly LiterateSerializer literateSerializer;

        private static readonly Interval ExecutionGrade = new Interval(10, 1);

        public ConsoleResultFormatter(LiterateSerializer literateSerializer)
        {
            this.literateSerializer = literateSerializer;

        }

        public ContainerSpan Format(int index, SuiteResult result)
        {
            var formattedErrors = FormatErrors(result.Errors);
            var formattedEventResults = ResultSection("Events", result.Results.Where(e => e is { Subject: Expectation }));
            var formattedAggregateResults = ResultSection("Segment tests", result.Results.Where(e => e is { Subject: AggregateExpectation }));

            var performance = result.Context.ExecutionTime.TotalSeconds;

            return new ContainerSpan(
                FormatSuccess(result.Success),
                Space,
                result.Context.Suite.Name.AsTextSpan(),
                index.ToString("\\.##0: ").AsTextSpan(),
                performance.ToString("{0.00 s} ").StyleGrade(performance, ExecutionGrade),
                "for ".AsTextSpan(),
                result.Context.ToolName.StyleValue(),
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
                    ($" - ").AsTextSpan(),
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
                    (
                        " " + assertion.Name
                        + (assertion.MatchedKey is null ? string.Empty : $" {assertion.MatchedKey}")
                    ).AsTextSpan()
                );

                if (assertion is FailedAssertion f)
                {
                    yield return ": ".AsTextSpan();
                    yield return string.Join(", ", f.Reasons).StyleFailure();
                }

            }
        }
    }
}