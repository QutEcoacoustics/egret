using Egret.Cli.Models;
using Microsoft.Extensions.Logging;
using StringTokenFormatter;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using System.Threading;
using Serilog;
using Egret.Cli.Extensions;
using System.Text.RegularExpressions;
using static LanguageExt.Prelude;
using LanguageExt;
using System.IO.Abstractions;

namespace Egret.Cli.Processing
{
    public class ToolRunner
    {
        private readonly ILogger<ToolRunner> logger;
        private readonly TimeSpan timeout;
        private readonly ArgumentQuoter tokenFormatter;
        private readonly TempFactory temp;

        public ToolRunner(ILogger<ToolRunner> logger, TempFactory temp, IOptions<AppSettings> appSettings)
        {
            this.temp = temp;
            this.logger = logger;
            timeout = appSettings.Value.ToolTimeout;
            tokenFormatter = new ArgumentQuoter();
        }

        public async Task<ToolResult> Run(Tool tool, FileInfo source, Dictionary<string, object> suiteConfig, IDirectoryInfo configDirectory)
        {
            var tempDir = temp.GetTempDir().Unwrap();
            var outputDir = tempDir;

            var placeholders = new CommandPlaceholders(source.FullName, outputDir.FullName, tempDir.FullName, configDirectory.FullName);
            var arguments = PrepareArguments(tool.Command, suiteConfig, placeholders);

            logger.LogTrace("Running process: {tool} {args}", tool.Executable, arguments);
            ProcessResult processResult;
            using (var timer = logger.Measure("Running process"))
            {
                processResult = await ExecuteShellCommand(tool.Executable, arguments, timeout, tempDir);
            }

            var version = GetVersion(tool.VersionRegex, processResult);


            logger.LogDebug("Finished process {tool} {args} with result {exitCode}", tool.Executable, arguments, processResult.ExitCode);
            logger.LogTrace("Process detail: {@process}", processResult);

            return new ToolResult(processResult.Success, outputDir, processResult, tool.Executable + " " + arguments, version);
        }

        public string PrepareArguments(string args, Dictionary<string, object> suiteConfig, CommandPlaceholders placeholders)
        {
            logger.LogTrace("Formatting args: {args} with parameters {@placeholders} and {@suiteConfig}", args, placeholders, suiteConfig);
            var standardPlaceholders = TokenValueContainer.FromObject(placeholders);
            var suitePlaceholders = TokenValueContainer.FromDictionary(suiteConfig);
            var compositePlaceholders = TokenValueContainer.Combine(
                standardPlaceholders,
                suitePlaceholders,
                ThrowOnMissingValueContainer.Instance);


            var parsedArgs = SegmentedString.Parse(args);
            var formattedArgs = parsedArgs.Format(compositePlaceholders, formatter: tokenFormatter);

            logger.LogTrace("Formatted args: {args}", formattedArgs);
            return formattedArgs;
        }

        public static async Task<ProcessResult> ExecuteShellCommand(string command, string arguments, TimeSpan timeout, DirectoryInfo tempDir)
        {
            using var process = new Process();

            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            // note: `ShellExecute` does not mean command line shell. It is a reference to a GUI
            // shell, like Windows Explorer - and we do not want to open interactive processes.
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = tempDir.FullName;


            var outputBuilder = new StringBuilder();

            var outputCloseEvent = new TaskCompletionSource<bool>();
            var timeoutToken = new CancellationTokenSource();


            bool isStarted;
            try
            {
                isStarted = process.Start();
            }
            catch (Exception ex)
            {
                // Usually it occurs when an executable file is not found or is not executable
                isStarted = false;
                return new ProcessResult(false, -1, string.Empty, string.Empty, ex);
            }


            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            timeoutToken.CancelAfter(timeout);
            var exitTask = process.WaitForExitAsync(timeoutToken.Token);

            TaskCanceledException cancelled = null;
            try
            {
                await exitTask;
                await Task.WhenAll(stdoutTask, stderrTask);
            }
            catch (TaskCanceledException ex)
            {
                cancelled = ex;
            }

            Debug.Assert(cancelled == null ? !exitTask.IsCanceled : exitTask.IsCanceled);
            if (exitTask.IsCanceled)
            {
                Exception killException = null;
                // timeout
                try
                {
                    // Kill hung process
                    process.Kill(true);
                }
                catch (Exception ex)
                {
                    killException = ex;
                }

                return new ProcessResult(false, -1, stdoutTask.Result, stderrTask.Result, cancelled.MakeAggregate(killException));
            }
            else if (exitTask.IsFaulted)
            {
                return new ProcessResult(true, process.ExitCode, stdoutTask.Result, stderrTask.Result, exitTask.Exception);
            }
            else
            {
                return new ProcessResult(true, process.ExitCode, stdoutTask.Result, stderrTask.Result, null);
            }
        }

        public static Option<string> GetVersion(Regex versionPattern, ProcessResult result)
        {
            if (versionPattern is null)
            {
                return None;
            }

            // one capture group + one implicit global group
            if (versionPattern.GetGroupNumbers().Length != 2)
            {
                throw new ArgumentException("Version regex requires exactly one capture group", nameof(versionPattern));
            }


            if (result.Output is not null)
            {
                var match = versionPattern.Match(result.Output);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            if (result.Error is not null)
            {
                var match = versionPattern.Match(result.Error);

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return None;
        }

        private class ArgumentQuoter : ITokenValueFormatter
        {
            public string Format(ISegment segment, object value, string Padding, string Format)
            {

                if (segment is TokenSegment &&
                    value is string s &&
                    s.Contains(' ') &&
                    !string.Equals(Format, "unquoted", StringComparison.InvariantCultureIgnoreCase))
                {
                    return $"\"{s}\"";
                }

                return TokenValueFormatter.Default.Format(segment, value, Padding, Format);
            }
        }

        private class ThrowOnMissingValueContainer : ITokenValueContainer
        {
            public static ITokenValueContainer Instance = new ThrowOnMissingValueContainer();
            public bool TryMap(IMatchedToken matchedToken, out object mapped)
            {
                // the idea here is that this container is the bottom of the stack.
                // if we got here all other containers failed and we're in an error case
                throw new ArgumentException(
                    $"Could not finish templating command. Missing a value for parameter `{matchedToken.Original}`");
            }
        }

    }

    public record ProcessResult
    {
        public ProcessResult(bool completed, int? exitCode, string output, string error, Exception exception)
        {
            Completed = completed;
            ExitCode = exitCode;
            Output = output;
            Error = error;
            Exception = exception;
        }

        public bool Completed { get; init; }
        public int? ExitCode { get; init; }
        public string Output { get; init; }
        public string Error { get; init; }

        public Exception Exception { get; init; }

        public bool Success => Completed && ExitCode == 0 && Exception is null;
    }

    public partial record ToolResult(bool Success, DirectoryInfo Results, ProcessResult ProcessResult, string TemplatedCommand, Option<string> Version);

    public partial record ToolResult
    {
        public string FormatError()
        {
            if (Success) { return string.Empty; }

            var builder = new StringBuilder();
            builder.Append($"Tool did not exit successfully ({ProcessResult.ExitCode}):");
            builder.AppendLine(ProcessResult.Exception?.Message);
            builder.AppendLine("Does the following command work by itself?");
            builder.AppendLine(TemplatedCommand);
            return builder.ToString();
        }
    }

    public record CommandPlaceholders(string Source, string Output, string TempDir, string ConfigDir);
}