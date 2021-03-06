using Egret.Cli.Processing;
using LanguageExt;
using System.Collections.Generic;
using Egret.Cli.Models.Results;

namespace Egret.Cli.Models
{
    public class CentroidExpectation : EventExpectation
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
            yield return TestBound("Centroid.Start", result.Start, Centroid.StartSeconds);
            yield return TestBound("Centroid.Low", result.Low, Centroid.LowHertz);
        }
    }
}