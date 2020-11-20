using Egret.Cli.Processing;
using LanguageExt;
using System.Collections.Generic;

namespace Egret.Cli.Models
{
    public class CentroidExpectation : Expectation
    {
        public override string Name { get; init; } = "Centroid location";

        public Centroid Centroid { get; init; }

        public override Validation<string, double> Distance(NormalizedResult result)
        {
            return result
                .Centroid
                .Map(centroid => (double)Maths.Distance.Euclidean(Centroid.ToCoordinates(), centroid));
        }

        public override IEnumerable<Assertion> TestBounds(NormalizedResult result)
        {
            yield return TestBound("Start", result.Start, Centroid.StartSeconds);
            yield return TestBound("Low", result.Low, Centroid.LowHertz);
        }
    }
}