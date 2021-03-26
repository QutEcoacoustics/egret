namespace Egret.Cli.Formatters
{
    using Extensions;
    using Models.Audacity;
    using Models.Results;
    using Processing;
    using Serialization.Audacity;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
        private readonly List<FileInfo> outputFiles;

        public IReadOnlyCollection<FileInfo> OutputFiles => new ReadOnlyCollection<FileInfo>(outputFiles);

        public AudacityResultFormatter(OutputFile outputFile, AudacitySerializer serializer)
        {
            this.outputFile = outputFile;
            this.serializer = serializer;
            this.outputFiles = new List<FileInfo>();
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
            var sourceFile = new FileInfo(result.Context.SourceName);
            var sourceName = sourceFile.Filestem();

            var outputBaseFile = outputFile.GetOutputFile(".aup");
            var outputName = outputBaseFile.Filestem();

            var outputFullName = outputName + "_" + sourceName;
            var outputFullFile = new FileInfo(Path.Combine(outputBaseFile.DirectoryName, outputFullName + ".aup"));

            this.outputFiles.Add(outputFullFile);

            // TODO: Goal is to output all information for one test file into one Audacity project file.
            // Include data about the suite, tool, type(segment|event)
            // each track shows the results for one tool's output (events only)
            // segment output is for the whole audio file, and can be stored in the tags

            // store the outcome and context
            var tags = new List<Tag>
            {
                new Tag("OverallOutcome",
                    result.Success ? "All expectations passed" : "One or more expectations failed"),
                new Tag(nameof(result.Context.SourceName), result.Context.SourceName),
                new Tag(nameof(result.Context.SuiteName), result.Context.SuiteName),
                new Tag(nameof(result.Context.TestName), result.Context.TestName),
                new Tag(nameof(result.Context.ToolName), result.Context.ToolName),
                new Tag(nameof(result.Context.ToolVersion), result.Context.ToolVersion),
                new Tag(nameof(result.Context.CaseTracker), result.Context.CaseTracker.ToString()),
                new Tag(nameof(result.Context.ExecutionIndex), result.Context.ExecutionIndex.ToString()),
                new Tag(nameof(result.Context.ExecutionTime), result.Context.ExecutionTime.ToString()),
                new Tag(nameof(result.Context.ExecutionTime), result.Context.ExecutionTime.ToString()),
            };

            tags.AddRange(result.Errors
                .Select((error, errorIndex) => new Tag($"Error{errorIndex:D2}", error)));

            // store the results
            var tracks = new List<LabelTrack>();

            // TODO: convert results to tracks and labels
            result.Results.Select(r => r);

            // build the audacity project
            var project = new Project
            {
                Tags = tags.ToArray(), Tracks = tracks.ToArray(), ProjectName = $"{outputFullName}_data",
            };
            return (outputFullFile, project);
        }
    }
}