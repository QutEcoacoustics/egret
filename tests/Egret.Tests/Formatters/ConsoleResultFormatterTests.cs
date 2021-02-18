using Egret.Cli.Formatters;
using Egret.Cli.Models.Results;
using System;
using Xunit;

namespace Egret.Tests.Formatters
{
    public class ConsoleResultFormatterTests
    {

        [Fact]
        public void CanFormatStatistics()
        {
            var stats = new BinaryStatistics()
            {
                ConditionPositives = 99,
                ConditionNegatives = 0,
                PredictedPositives = 0,
                TruePositives = 34,
                FalsePositives = 0,
                PredictedNegatives = 0,
                FalseNegatives = 65,
                TrueNegatives = 0
            };


            var expected = new string[] {
                "test title:",
                "                   Labelled +ve        Labelled -ve                                               ",
                "Total segments: 99                  99                0 Prevalance:      100.00% Accuracy: 34.34% ",
                "Results +ve:     0 TP:              34 FP:            0 Precision (PPV):       ∞ FDR:         NaN ",
                "Results -ve:     0 FN:              65 TN:            0 FOR:                   ∞ NPV:         NaN ",
                "Results Count:  99 Sensitivity: 34.34% FPR:         NaN                                           ",
                "                   FNR:              ∞ Specificity: NaN                                           ",
            }.Join(Environment.NewLine) + Environment.NewLine;

            var formatted = ConsoleResultFormatter.FormatStatistics("test title", "segments", stats);

            var actual = formatted.ToString(System.CommandLine.Rendering.OutputMode.PlainText);

            Assert.Equal(expected, actual);
        }

    }
}