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

namespace Egret.Cli.Processing
{
    public class ToolRunner
    {
        private readonly ILogger<ToolRunner> logger;
        private readonly TimeSpan timeout;
        private readonly TempFactory temp;

        public ToolRunner(ILogger<ToolRunner> logger, TempFactory temp, IOptions<AppSettings> appSettings)
        {
            this.temp = temp;
            this.logger = logger;
            this.timeout = appSettings.Value.ToolTimeout;

        }

        public async Task<ToolResult> Run(Tool tool, FileInfo source, Dictionary<string, object> suiteConfig)
        {
            var tempDir = temp.GetTempDir();
            var outputDir = tempDir;

            var placeholders = new CommandPlaceholders(source.FullName, outputDir.FullName, tempDir.FullName);
            var arguments = PrepareArguments(tool.Command, suiteConfig, placeholders);

            logger.LogTrace("Starting process: {tool} {args}", tool.Executable, arguments);
            var stopWatch = Stopwatch.StartNew();
            var processResult = await ExecuteShellCommand(tool.Executable, arguments, timeout, tempDir);
            stopWatch.Stop();

            logger.LogDebug("Finished process {tool} {args} with result {exitCode}", tool.Executable, arguments, processResult.ExitCode);
            logger.LogTrace("Process detail: {@process}", processResult);

            return new ToolResult(processResult.Success, outputDir, processResult.Exception);
        }

        public string PrepareArguments(string args, Dictionary<string, object> suiteConfig, CommandPlaceholders placeholders)
        {
            logger.LogTrace("Formatting args: {args} with parameters {@placeholders} and {@suiteConfig}", args, placeholders, suiteConfig);
            var standardPlaceholders = TokenValueContainer.FromObject(placeholders);
            var suitePlaceholders = TokenValueContainer.FromDictionary(suiteConfig);
            var compositePlaceholders = TokenValueContainer.Combine(standardPlaceholders, suitePlaceholders);


            var parsedArgs = SegmentedString.Parse(args);
            var formattedArgs = parsedArgs.Format(compositePlaceholders);

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

    }

    public record ToolResult
    {
        public ToolResult(bool success, DirectoryInfo results, Exception exception)
        {
            Success = success;
            Results = results;
            Exception = exception;
        }

        public bool Success { get; }

        public DirectoryInfo Results { get; }
        public Exception Exception { get; }
    }

    public record CommandPlaceholders(string Source, string Output, string TempDir);
}