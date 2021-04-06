namespace Egret.Cli.Formatters
{
    using Models.Audacity;
    using Models.Results;
    using Processing;
    using Serialization.Audacity;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Result formatter that stores output in Audacity (2.x) project files.
    /// </summary>
    public class AudacityResultFormatter : IResultFormatter
    {
        private readonly OutputFile outputFile;
        private readonly AudacitySerializer serializer;

        public AudacityResultFormatter(OutputFile outputFile, AudacitySerializer serializer)
        {
            this.outputFile = outputFile;
            this.serializer = serializer;
        }

        public ValueTask DisposeAsync()
        {
            // no op
            return ValueTask.CompletedTask;
        }

        public ValueTask WriteResultsHeader()
        {
            // no op
            return ValueTask.CompletedTask;
        }

        public ValueTask WriteResult(int index, TestCaseResult result)
        {
            (FileInfo currentFile, Project project) = this.BuildProject(index, result);
            this.serializer.Serialize(currentFile.FullName, project);

            return ValueTask.CompletedTask;
        }

        public ValueTask WriteResultsFooter(FinalResults finalResults)
        {
            // TODO: what to do with the stats?
            // for now, just ignore the footer data
            return ValueTask.CompletedTask;
        }

        private (FileInfo, Project) BuildProject(int index, TestCaseResult result)
        {
            var sourceName = Path.GetFileNameWithoutExtension(result.Context.SourceName);
            var resultFile = outputFile.GetOutputFile(".aup", sourceName);

            // store the outcome and context
            var outcome = result.Success ? "All expectations passed" : "One or more expectations failed";
            var tags = new List<Tag>
            {
                new("OverallOutcome", outcome),
                new("ResultIndex", index.ToString()),
                new(nameof(result.Context.SourceName), result.Context.SourceName),
                new(nameof(result.Context.SuiteName), result.Context.SuiteName),
                new(nameof(result.Context.TestName), result.Context.TestName),
                new(nameof(result.Context.ToolName), result.Context.ToolName),
                new(nameof(result.Context.ToolVersion), result.Context.ToolVersion),
                new(nameof(result.Context.CaseTracker), result.Context.CaseTracker.ToString()),
                new(nameof(result.Context.ExecutionIndex), result.Context.ExecutionIndex.ToString()),
                new(nameof(result.Context.ExecutionTime), result.Context.ExecutionTime.ToString()),
            };

            // record any errors
            tags.AddRange(result.Errors.Select((error, errorIndex) =>
                new Tag($"Error{errorIndex:D2}", error))
            );

            // convert results to tracks and labels
            var eventTruePositives = new List<Label>();
            var eventFalsePositives = new List<Label>();
            var eventTrueNegatives = new List<Label>();
            var eventFalseNegatives = new List<Label>();

            foreach (var expectationResult in result.Results)
            {
                // TODO: add event expectation result to TP, FP, TN, FN label list
                // TODO: add segment-level result as a tag

                var isSuccessful = expectationResult.Successful;
                var isSegment = expectationResult.IsSegmentResult;

                var subject = expectationResult.Subject;
                var subjectName = subject.Name ?? String.Empty;
                var isPositiveAssertion = subject.IsPositiveAssertion;

                var target = expectationResult.Target;

                var truePositive = expectationResult.Contingency == Contingency.TruePositive;
                var falsePositive = expectationResult.Contingency == Contingency.FalsePositive;
                var trueNegative = expectationResult.Contingency == Contingency.TrueNegative;
                var falseNegative = expectationResult.Contingency == Contingency.FalseNegative;

                foreach (var assertion in expectationResult.Assertions)
                {
                    switch (assertion)
                    {
                        case SuccessfulAssertion successfulAssertion:

                            break;
                        case ErrorAssertion errorAssertion:

                            break;
                        case FailedAssertion failedAssertion:

                            break;
                        default:
                            throw new NotImplementedException(
                                $"Unable to process assertion of type '{assertion.GetType()}'.");
                    }
                }
            }

            // create 4 tracks for TP, FP, TN, FN labels
            var tracks = new List<LabelTrack>
            {
                new("EvTP", 1, 100, 0, eventTruePositives.ToArray()),
                new("EvFP", 1, 100, 0, eventFalsePositives.ToArray()),
                new("EvTN", 1, 100, 0, eventTrueNegatives.ToArray()),
                new("EvFN", 1, 100, 0, eventFalseNegatives.ToArray()),
            };

            // build the audacity project
            var project = new Project
            {
                Tags = tags.ToArray(),
                Tracks = tracks.ToArray(),
                ProjectName = $"{sourceName}_data",
                Version = "1.3.0",
                AudacityVersion = "2.4.2",
                Rate = 44100,
                SnapTo = "off",
                SelectionFormat = "hh:mm:ss + milliseconds",
                FrequencyFormat = "Hz",
                BandwidthFormat = "octaves"
            };

            return (resultFile, project);
        }
    }
}