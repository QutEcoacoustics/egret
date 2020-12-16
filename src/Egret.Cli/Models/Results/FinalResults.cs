using Egret.Cli.Processing;
using LanguageExt;
using System;

namespace Egret.Cli.Models.Results
{

    public record FinalResults(
        Config Config,
        ResultsStatistics ResultStatistics,
        TimeSpan TimeTaken
    );

    public record TestStats(int Count, int Successes, int Failures);
}