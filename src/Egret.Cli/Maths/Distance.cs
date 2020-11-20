using System;
using System.Numerics;




namespace Egret.Cli.Maths
{
    public static class Distance
    {
        /// <summary>
        /// Euclidean Distance, i.e. the L2-norm of the difference.
        /// </summary>
        public static float Euclidean(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }

        public static float Euclidean(float a, float b)
        {
            return MathF.Sqrt(MathF.Pow(a - b, 2));
        }

        /// <summary>
        /// Measures the distance between box a (represented by (a1, a2) to box b (b1, b2))
        /// by summing the distance between the closest vertices.
        /// </summary>
        /// <param name="a1">bottom left of a</param>
        /// <param name="b1">bottom left of b</param>
        /// <param name="a2">top right of a</param>
        /// <param name="b2">top right of b</param>
        /// <returns>the sum of distances between a and b. 0 represents no distance (the boxes share the same coordinates).<returns>
        public static float BoxDistance((Vector2 BotLeft, Vector2 TopRight) a, (Vector2 BotLeft, Vector2 TopRight) b)
        {
            var a1b1 = Euclidean(a.BotLeft, b.BotLeft);
            var a2b2 = Euclidean(a.TopRight, b.TopRight);

            var distance = a1b1 + a2b2;

            return distance;

        }

        public static float RangeDistance(float a1, float a2, float b1, float b2)
        {
            // measures distance between two points in one dimension to another two
            // a:      |---------|
            //         a1        a2
            // b:          |-------| 
            //             b1      b2

            var firstDist = Euclidean(a1, b1);
            var secondDist = Euclidean(a2, b2);

            var distance = firstDist + secondDist;

            return distance;

        }
    }
}