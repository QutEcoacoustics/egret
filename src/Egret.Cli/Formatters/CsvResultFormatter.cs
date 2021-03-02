using CsvHelper;
using CsvHelper.Configuration;
using Egret.Cli.Commands;
using Egret.Cli.Extensions;
using Egret.Cli.Models.Results;
using Egret.Cli.Processing;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;
using MoreLinq;
using CsvHelper.Configuration.Attributes;
using System.Threading;
using Nito.AsyncEx;
using System;

namespace Egret.Cli.Formatters
{
    public class CsvResultFormatter : IResultFormatter
    {
        private readonly FileInfo output;
        private readonly StreamWriter stream;
        private readonly CsvWriter writer;
        private readonly AsyncLock @lock;

        public CsvResultFormatter(OutputFile outputFile)
        {
            output = outputFile.GetOutputFile("csv");
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                //;  ReferenceHeaderPrefix = args => $"{args.MemberName}.",

            };

            stream = output.CreateText();
            writer = new CsvWriter(stream, configuration);
            writer.Context.RegisterClassMap(new CsvResultCsvMap(writer.Context));
            @lock = new AsyncLock();
        }

        public async ValueTask DisposeAsync()
        {
            await writer.DisposeAsync();
            await stream.DisposeAsync();
        }

        public ValueTask WriteResult(int index, TestCaseResult result)
        {
            // no-op: CSV format will only show summary data
            return default;
        }

        public async ValueTask WriteResultsFooter(FinalResults finalResults)
        {
            using (await @lock.LockAsync())
            {
                foreach (var row in CsvResult.Create(finalResults))
                {
                    writer.WriteRecord(row);
                    await writer.NextRecordAsync();
                }
            }
        }

        public async ValueTask WriteResultsHeader()
        {
            using var _ = await @lock.LockAsync();
            writer.WriteHeader<CsvResult>();
            await writer.NextRecordAsync();
        }

        private record CsvResult
        {
            public CsvResult(FinalResults results, string suite, string tool, string type, BinaryStatistics stats)
            {
                Results = results;
                Suite = suite;
                Tool = tool;
                Type = type;
                Stats = stats;
            }

            [Ignore]
            public FinalResults Results { get; }

            public string Suite { get; }

            public string Tool { get; }

            public string Type { get; }

            public BinaryStatistics Stats { get; }

            public static IEnumerable<CsvResult> Create(FinalResults final)
            {
                //final.ResultStatistics.Stats.enu
                foreach (var (suite, tool, type, stats) in final.ResultStatistics.Stats.Flatten())
                {
                    yield return new CsvResult(
                        final,
                        suite,
                        tool,
                        type,
                        stats
                    );
                }
            }
        }

        private class CsvResultCsvMap : ClassMap<CsvResult>
        {
            public CsvResultCsvMap(CsvContext context)
            {
                base.AutoMap(context);
                base.Map(x => x.Results.TimeTaken).Convert(args => args.Value.Results.TimeTaken.TotalSeconds.ToString());
                base.Map(x => x.Results.Config.Location.FullName).Name("ConfigFile");
            }
        }
    }


}