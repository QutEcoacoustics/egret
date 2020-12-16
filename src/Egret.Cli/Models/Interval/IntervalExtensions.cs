namespace Egret.Cli.Models
{
    public static class IntervalExtensions
    {
        public static bool IntersectsWith(this double value, Interval interval) => interval.Contains(value);

        public static Interval WithTolerance(this double value, double tolerance)
        {
            return Interval.FromTolerance(value, tolerance);
        }
    }
}