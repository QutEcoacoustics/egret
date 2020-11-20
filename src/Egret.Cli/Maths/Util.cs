namespace Egret.Cli.Maths
{
    public static class Util
    {

        public static double CenteredBetween(this double a, double b)
        {
            return ((a > b ? (a - b) : (b - a)) * 0.5) + a;
        }

        public static double Center(this in (double, double) ab)
        {
             var (a, b) = ab;
            return ((a > b ? (a - b) : (b - a)) * 0.5) + a;
        }
    }
}