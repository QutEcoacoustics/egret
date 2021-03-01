using LanguageExt;
using static LanguageExt.Prelude;
using Egret.Cli.Extensions;
using Egret.Cli.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipelines;
using Egret.Cli.Serialization.Json;
using System.Text.Json;
using Egret.Cli.Models.Results;
using Egret.Cli.Models.Expectations;
using Egret.Cli.Models.AnalysisOutput;
using Egret.Cli.Serialization.Avianz;
using MoreLinq;
using LanguageExt.ClassInstances;
using MathNet.Numerics.LinearAlgebra;
using SixLabors.ImageSharp;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Egret.Cli.Processing
{
    public class CaseExecutor : IAsyncInvokeable<TestCaseResult>, IDisposable
    {

        public readonly struct CaseTracker
        {
            public CaseTracker(ushort testCase, ushort tool, ushort suite)
            {
                TestCase = testCase;
                Tool = tool;
                Suite = suite;
            }

            ushort TestCase { get; }
            ushort Tool { get; }
            ushort Suite { get; }

            public override string ToString()
            {
                return $"{Suite}.{Tool}.{TestCase}";
            }
        }

        public CaseExecutor(
            ILogger<CaseExecutor> logger,
            ToolRunner runner,
            TempFactory tempFactory,
            HttpClient httpClient,
            DefaultJsonSerializer resultDeserializer,
            AvianzDeserializer avianzDeserializer)

        {
            this.logger = logger;
            this.tool = runner;
            this.tempFactory = tempFactory;
            this.http = httpClient;
            this.resultDeserializer = resultDeserializer;
            this.avianzDeserializer = avianzDeserializer;
        }

        public TestCase Case { get; init; }
        public Suite Suite { get; init; }
        public Tool Tool { get; init; }

        public CaseTracker Tracker { get; init; }
        public Config Config { get; init; }

        private readonly ILogger<CaseExecutor> logger;
        private readonly ToolRunner tool;
        private readonly TempFactory tempFactory;
        private readonly HttpClient http;
        private readonly DefaultJsonSerializer resultDeserializer;
        private readonly AvianzDeserializer avianzDeserializer;

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
                Option<string> toolVersion = default;
                try
                {
                    var (file, newSource) = await ResolveSourceAsync();
                    source = newSource ?? source;

                    var toolResult = await RunToolAsync(file);
                    if (!toolResult.Success)
                    {
                        errors.Add(toolResult.FormatError());
                    }
                    else
                    {
                        var analysisresults = await LoadResultsAsync(toolResult).ToListAsync(token);

                        logger.LogInformation("Loaded {count} results", analysisresults.Count);

                        expectationResults = AssessResults(Case.Expect, analysisresults);
                    }
                    toolVersion = toolResult.Version;

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
                        Suite.Name,
                        Case.Name ?? string.Empty,
                        Tool.Name,
                        toolVersion.IfNoneUnsafe((string)null),
                        source,
                        index,
                        Tracker,
                        timer.Stop()
                    )
                ));
            }
        }

        private async ValueTask<(FileInfo File, string updatedSource)> ResolveSourceAsync()
        {
            return Case switch
            {
                TestCase { File: not null } c => ResolveLocal(c.File),
                TestCase { Uri: not null } c => await FetchRemoteHttpFileAsync(c.Uri),
                _ => throw new InvalidOperationException($"Can't determine case source file")
            };

            (FileInfo File, string updatedSource) ResolveLocal(string path)
            {
                if (!Path.IsPathFullyQualified(path))
                {
                    // nominally things are relative the config file
                    // however for imported test cases the config file is virtual...
                    // so we use the case source as an indication of the virtual (or real) config file
                    path = Path.Combine(Path.GetDirectoryName(Case.SourceInfo.Source), path);
                }

                path = Path.GetFullPath(path);

                FileInfo file = new FileInfo(path);
                if (file.Exists)
                {
                    return (file, Path.GetRelativePath(Path.GetDirectoryName(Suite.SourceInfo.Source), file.FullName));
                }

                throw new FileNotFoundException($"File not found: {path}", file.FullName);
            }
        }

        private async Task<(FileInfo, string)> FetchRemoteHttpFileAsync(Uri uri)
        {
            var downloadDest = tempFactory.GetTempFile().Unwrap();
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

            return (downloadDest, null);
        }

        private async ValueTask<ToolResult> RunToolAsync(FileInfo file)
        {
            var suiteConfigForTool = Suite.ToolConfigs.GetValueOrDefault(Tool.Name);
            var configDirectory = Config.Location.Directory;
            var result = await tool.Run(Tool, file, suiteConfigForTool, configDirectory);
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

            int index = 0;
            foreach (var file in files)
            {


                var sourceInfo = new SourceInfo(file.FullName);
                // determine format
                switch (file.Extension.ToLower())
                {
                    case ".json":
                        using (var stream = File.OpenRead(file.FullName))
                        using (var document = await JsonDocument.ParseAsync(stream))
                        {
                            if (document.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in document.RootElement.EnumerateArray())
                                {
                                    if (item.ValueKind == JsonValueKind.Object)
                                    {
                                        yield return new JsonResult(index, item, sourceInfo);
                                    }
                                }
                            }
                            else
                            {
                                throw new ArgumentException("Results did not contain an array as root element");
                            }
                        }
                        break;
                    case ".data":
                        // avianz result files
                        var labels = await avianzDeserializer.DeserializeLabelFile(file.FullName);
                        foreach (var label in labels.Annotations)
                        {
                            yield return new AvianzResult(index, label, sourceInfo);
                        }
                        break;
                    default:
                        throw new NotSupportedException($"Cannot result files of type {file.Extension} yet (for `{file}`)");
                }

                index++;
            }

            if (index > 0)
            {
                logger.LogWarning("No result files were found, can't assess any results");
            }
        }

        private List<ExpectationResult> AssessResults(IReadOnlyList<IExpectation> expectations, IReadOnlyList<NormalizedResult> actual)
        {
            logger.LogTrace("Assesing {count} expectations", expectations.Count);

            return ExpectationAssessment.AssessResults(expectations, actual, Suite);
        }



        public void Dispose()
        {
            http.Dispose();
            GC.SuppressFinalize(this);
        }
    }


}