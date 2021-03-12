using Egret.Cli.Models;
using Egret.Cli.Models.Results;
using LanguageExt;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;

namespace Egret.Cli.Processing
{
    public class ResultsStatistics
    {
        public ResultsStatistics()
        {
        }

        public record EventSegmentStats
        {
            public EventSegmentStats()
            {
            }

            public Dictionary<string, BinaryStatistics> EventStats { get; } = new();
            public Dictionary<string, BinaryStatistics> SegmentStats { get; } = new();


        }

        // need to sort statistics into suite+tool+[segment|event] buckets
        public Map<string, Map<string, Map<string, BinaryStatistics>>> Stats { get; private set; } = new();
        public Map<string, Map<string, int>> ProcessingErrors { get; private set; } = new();

        public const string EventsKey = "Events";
        public const string SegmentsKey = "Segments";

        public static readonly string[] Keys = new string[] { EventsKey, SegmentsKey };


        public int TotalResults { get; private set; }
        public int TotalSuccesses { get; private set; }
        public int TotalFailures { get; private set; }

        public int TotalErrors { get; private set; }

        public BinaryStatistics GrandTotalSegments { get; private set; } = BinaryStatistics.Empty;
        public BinaryStatistics GrandTotalEvents { get; private set; } = BinaryStatistics.Empty;

        public void ProcessRecord(TestCaseResult result)
        {
            // each test case results represnets a single source run with a single tool, for a single suite
            var suiteKey = result.Context.SuiteName;
            var toolKey = result.Context.ToolName;

            if (result.Errors.Count >= 1)
            {
                TotalErrors++;
                ProcessingErrors = ProcessingErrors.AddOrUpdate(suiteKey, toolKey, (count) => count + 1, () => 1);
                TotalResults++;
            }

            // there can be multiple expectations in each segment
            // i.e. segment-level results like "no events in this segment"
            // i.e. event-level result like "Koala found at [12.5, 400, 30.0, 1300]"
            // for each determine if they're true/false and add them to appropriate statistics
            foreach (var expectationResult in result.Results)
            {
                var isSegment = expectationResult.IsSegmentResult;

                var current = Get();
                var one = expectationResult.Contingency.Case switch
                {
                    null when expectationResult.Subject.IsPositiveAssertion => BinaryStatistics.OneErroredPositive,
                    null when !expectationResult.Subject.IsPositiveAssertion => BinaryStatistics.OneErroredNegative,
                    Contingency contingency => contingency,
                    _ => throw new NotImplementedException(),
                };
                Set(current + one);

                // grand totals
                if (isSegment)
                {
                    GrandTotalSegments += one;
                }
                else
                {
                    GrandTotalEvents += one;
                }

                // other stats
                TotalResults++;
                switch (result.Success)
                {
                    case true: TotalSuccesses++; break;
                    case false: TotalFailures++; break;
                }

                BinaryStatistics Get()
                {
                    return Stats.Find(suiteKey, toolKey, isSegment ? SegmentsKey : EventsKey).IfNone(BinaryStatistics.Empty);
                }

                void Set(BinaryStatistics value)
                {
                    Stats = Stats.AddOrUpdate(suiteKey, toolKey, isSegment ? SegmentsKey : EventsKey, value);
                }
            }

            // TODO: test all cases counted correctly
            // 8 cases to cover
            // segment & TP:
            //   - label presence & label:A  & match:true  & A found
            //   -  no events                & match:false & >0 events
            //   - event count    & count:>0 & match:true  & >0 events
            // segment & FP:
            //   - label presence & label:A  & match:false & A found
            //   - no events                 & match:true  & >0 events
            //   - event count    & count:>0 & match:false & >0 events
            // segment & TN: 
            //   - label presence & label:A  & match:false & A NOT found
            //   - no events                 & match:true  & 0 events
            //   - event count    & count:>0 & match:false & 0 events
            // segment & FN: 
            //   - label presence & label:A  & match:true  & A NOT found
            //   - no events                 & match:false & 0 events
            //   - event count    & count:>0 & match:true  & 0 events

        }
    }
}