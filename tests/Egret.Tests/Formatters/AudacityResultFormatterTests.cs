namespace Egret.Tests.Formatters
{
    using Cli.Commands;
    using Cli.Formatters;
    using Cli.Hosting;
    using Cli.Models.Results;
    using Cli.Processing;
    using Support;
    using System;
    using System.IO;
    using Xunit;
    using Xunit.Abstractions;

    public class AudacityResultFormatterTests : TestBase
    {
        public AudacityResultFormatterTests(ITestOutputHelper output) : base(output)
        {
        }
        
        [Fact]
        public async void TestNoResults()
        {
            // arrange
            var formatter = GetFormatter();
            var finalResults = GetFinalResults();
            
            // act
            await formatter.WriteResultsHeader();
            await formatter.WriteResultsFooter(finalResults);
            
            // assert
            // TODO
        }
        
        [Fact]
        public async void TestWriteResults()
        {
            // arrange
            var formatter = GetFormatter();
            var results = new TestCaseResult[]
            {
                new(Array.Empty<string>(), Array.Empty<ExpectationResult>(), new TestContext(null,
                    null,
                    null,
                    null,
                    null,
                    0,
                    new CaseExecutor.CaseTracker(),
                    TimeSpan.Zero))
            };
            var finalResults = GetFinalResults();
            
            
            // act
            await formatter.WriteResultsHeader();
            for (int i = 0; i < results.Length; i++)
            {
                await formatter.WriteResult(i,results[i]);
            }
            
            await formatter.WriteResultsFooter(finalResults);
            
            // assert
            // TODO
        }

        private AudacityResultFormatter GetFormatter()
        {
            var runInfo = new RunInfo(DateTime.Now);
            var options = new TestCommandOptions
            {
                Configuration = new FileInfo(this.TempFactory.GetTempFile("config.yml", true).FullName)
            };
            var outputFile = new OutputFile(options, runInfo);
            return new AudacityResultFormatter(outputFile, this.AudacitySerializer);
        }

        private FinalResults GetFinalResults()
        {
            return new(null, null, TimeSpan.Zero);
        }
    }
}