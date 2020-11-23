using LanguageExt;
using Egret.Cli.Extensions;
using Egret.Cli.Hosting;
using Egret.Cli.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipelines;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Egret.Cli.Processing
{
    public class CaseExecutor : IAsyncInvokeable<TestCaseResult>
    {

        public readonly struct CaseTracker
        {
            public CaseTracker(ushort @case, ushort tool, ushort suite)
            {
                Case = @case;
                Tool = tool;
                Suite = suite;
            }

            ushort Case { get; }
            ushort Tool { get; }
            ushort Suite { get; }

            public override string ToString()
            {
                return $"{Suite}.{Tool}.{Case}";
            }
        }

        public CaseExecutor(ILogger<CaseExecutor> logger, Hosting.EgretConsole console, ToolRunner runner, TempFactory tempFactory, HttpClient httpClient)
        {
            this.logger = logger;
            this.console = console;
            this.tool = runner;
            this.tempFactory = tempFactory;
            this.http = httpClient;
        }

        public Case Case { get; init; }
        public Suite Suite { get; init; }
        public Tool Tool { get; init; }

        public CaseTracker Tracker { get; init; }


        private readonly ILogger<CaseExecutor> logger;
        private readonly EgretConsole console;
        private readonly ToolRunner tool;
        private readonly TempFactory tempFactory;
        private readonly HttpClient http;

        public async Task<TestCaseResult> InvokeAsync(int index, CancellationToken token)
        {
            using (logger.BeginScope("Case {index}: {case}", index, Tracker))
            using (var timer = logger.Measure(Tracker.ToString()))
            {
                var source = Case.File ?? Case.Uri.ToString();
                logger.LogTrace("Starting case: {case} for tool: {tool}", source, Tool.Name);

                var errors = new List<string>();
                List<ExpectationResult> expectationResults = new();
                bool success;
                try
                {
                    var file = await ResolveSourceAsync();

                    var toolResult = await RunToolAsync(file);
                    if (!toolResult.Success)
                    {
                        errors.Add("tool did not exit successfully:" + toolResult.Exception?.Message);
                    }
                    else
                    {
                        var analysisresults = await LoadResultsAsync(toolResult).ToListAsync();

                        logger.LogInformation("Loaded {count} results", analysisresults.Count);

                        expectationResults = AssessResults(Case.Expect, analysisresults);

                    }

                    success = toolResult.Success && expectationResults.All(x => x.Successful);
                }
                catch (FileNotFoundException ex)
                {
                    logger.LogError("Failed to find file", ex);
                    errors.Add(ex.Message);
                    success = false;
                }



                logger.LogTrace("Finished case: {case} for tool: {tool}", source, Tool.Name);


                return await Task.FromResult(new TestCaseResult(
                    errors,
                    expectationResults,
                    new TestContext(
                        Suite,
                        Tool.Name,
                        source,
                        timer.Stop()
                    )
                ));
            }
        }

        private async ValueTask<FileInfo> ResolveSourceAsync()
        {
            return Case switch
            {
                Case { File: not null } c => ResolveLocal(c.File),
                Case { Uri: not null } c => await FetchRemoteHttpFileAsync(c.Uri),
                _ => throw new InvalidOperationException($"Can't determine case source file")
            };

            FileInfo ResolveLocal(string path)
            {
                if (!Path.IsPathFullyQualified(path))
                {
                    path = Path.Combine(Path.GetDirectoryName(Suite.Location), path);
                }

                path = Path.GetFullPath(path);

                FileInfo file = new FileInfo(path);
                if (file.Exists)
                {
                    return file;
                }

                throw new FileNotFoundException($"File not found: {path}", file.FullName);
            }
        }

        private async Task<FileInfo> FetchRemoteHttpFileAsync(Uri uri)
        {
            var downloadDest = tempFactory.GetTempFile();
            logger.LogTrace("Downloading {uri} to {tempFile}", uri, downloadDest);

            // todo: cache downloads

            // try and fetch the file
            // but only read the headers in the response - don't download the entire
            // body into memory.
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            using var fileStream = downloadDest.OpenWrite();
            var writer = PipeWriter.Create(fileStream);
            var reader = PipeReader.Create(response.Content.ReadAsStream());
            await reader.CopyToAsync(writer);

            return downloadDest;
        }

        private async ValueTask<ToolResult> RunToolAsync(FileInfo file)
        {
            var suiteConfigForTool = Suite.ToolConfigs.GetValueOrDefault(Tool.Name);
            var result = await tool.Run(this.Tool, file, suiteConfigForTool);
            logger.LogInformation("Tool result for {file}: {toolResult} ({output})", file.Name, result.Success, result.Results.FullName);
            return result;
        }

        private async IAsyncEnumerable<NormalizedResult> LoadResultsAsync(ToolResult result)
        {
            var files = result.Results.EnumerateFiles(Tool.ResultPattern, new EnumerationOptions()
            {
                MatchCasing = MatchCasing.CaseInsensitive,
                MatchType = MatchType.Simple,
                RecurseSubdirectories = true
            });

            foreach (var file in files)
            {
                // determine format
                switch (file.Extension.ToLower())
                {
                    case ".json":
                        using (var fileStream = file.OpenText())
                        using (var reader = new JsonTextReader(fileStream))
                        {
                            var root = await JToken.ReadFromAsync(reader);
                            if (root is JArray array)
                            {
                                foreach (var item in array)
                                {
                                    if (item is JObject jObject)
                                    {
                                        yield return new JsonResult(jObject);
                                    }
                                }
                            }
                            else
                            {
                                throw new ArgumentException("Results did not contain an array as root element");
                            }
                        }
                        break;
                    default:
                        throw new NotSupportedException($"Cannot result files of type {file.Extension} yet (for `{file}`)");
                }
            }

            // TODO: ADD WARNING WHEN NO FILES FOUND
        }

        private List<ExpectationResult> AssessResults(IExpectationTest[] expectations, IReadOnlyList<NormalizedResult> actual)
        {
            var results = new List<ExpectationResult>(expectations.Length);

            foreach (var expectation in expectations)
            {
                results.AddRange(expectation.Test(actual));
            }

            return results;
        }


    }
}