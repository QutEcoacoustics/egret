using Egret.Cli.Processing;
using LanguageExt;
using System.Collections.Generic;
using Egret.Cli.Models.Results;

namespace Egret.Cli.Models
{
    public class TemporalExpectation : EventExpectation
    {
        public TemporalExpectation() : base()
        {
        }

        public TemporalExpectation(object context) : base(context)
        {
        }

        public override string Name { get; init; } = "Time range";

        public TimeRange Time { get; init; }

        public override Validation<string, double> Distance(NormalizedResult result)
        {
            return result
                .Time
                .Map(time => (double)Maths.Distance.RangeDistance(
                    (float)Time.StartSeconds.Middle,
                    (float)Time.EndSeconds.Middle,
                    (float)time.Start,
                    (float)time.End
                ));
        }



        public override IEnumerable<Assertion> TestBounds(NormalizedResult result)
        {
            yield return TestBound("Time.Start", result.Start, Time.StartSeconds);
            yield return TestBound("Time.End", result.Low, Time.EndSeconds);
        }
    }
}