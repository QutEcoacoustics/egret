namespace Egret.Cli.Models
{
    public enum Topology : byte
    {
        /*
         * Our flags are defined like this to ensure the default value is [a,b).
         * The meaning of bit 1 is: Is the left exclusive? (1 yes, 0 no)
         * The meaning of bit 2 is: Is the right inclusive? (1 yes, 0 no)
         *
         * Note bit 1 is on the right.
         */

        /// <summary> Endpoints not included ( min < x < max ) </summay>
        Open = 0b0_1,

        /// <summary> Lower endpoint is included, upper is not ( min ≤ x < max ) </summay>
        LeftClosedRightOpen = 0b0_0,

        /// <summary> Lower endpoint is not included, upper is ( min < x ≤ max ) </summay>
        LeftOpenRightClosed = 0b1_1,

        /// <summary> Endpoints are included ( min ≤ x ≤ max ) </summay>
        Closed = 0b1_0,


        /// <summary> Endpoints not included ( min < x < max ) </summay>
        Exclusive = Open,

        /// <summary> Lower endpoint is included, upper is not ( min ≤ x < max ) </summay>
        MinimumInclusiveMaximumExclusive = LeftClosedRightOpen,

        /// <summary> Lower endpoint is not included, upper is ( min < x ≤ max ) </summay>
        MinimumExclusiveMaximumInclusive = LeftOpenRightClosed,

        /// <summary> Endpoints are included ( min ≤ x ≤ max ) </summay>
        Inclusive = Closed,

        /// <summary> Lower endpoint is included, upper is not ( min ≤ x < max ) </summay>
        Default = LeftClosedRightOpen,
    }

    public static class TopologyExtensions
    {
        public static bool IsMinimumInclusive(this Topology topology)
        {
            return topology is Topology.MinimumInclusiveMaximumExclusive or Topology.Inclusive;
        }

        public static bool IsMaximumInclusive(this Topology topology)
        {
            return topology is Topology.MinimumExclusiveMaximumInclusive or Topology.Inclusive;
        }


        /// <summary>
        /// Determines if two adjacent topologies would allow their endpoints to be
        /// equal if the endpoints had the same value.
        /// </summary>
        /// <param name="left">The topology of the interval that is lower than the other.</param>
        /// <param name="right"The topology of the interval that is greater than the other.></param>
        /// <returns></returns>
        public static bool IsCompatibleWith(this Topology left, Topology right)
        {
            return left.IsMaximumInclusive() || right.IsMinimumInclusive();
        }

        public static Topology CloseMinimum(this Topology topology)
        {
            return topology switch
            {
                Topology.MinimumInclusiveMaximumExclusive or Topology.Inclusive => topology,
                Topology.MinimumExclusiveMaximumInclusive => Topology.Inclusive,
                Topology.Exclusive => Topology.MinimumInclusiveMaximumExclusive,
                _ => throw new System.InvalidOperationException(),
            };
        }

        public static Topology CloseMaximum(this Topology topology)
        {
            return topology switch
            {
                Topology.MinimumExclusiveMaximumInclusive or Topology.Inclusive => topology,
                Topology.MinimumInclusiveMaximumExclusive => Topology.Inclusive,
                Topology.Exclusive => Topology.MinimumExclusiveMaximumInclusive,
                _ => throw new System.InvalidOperationException(),
            };
        }

        public static Topology OpenMinimum(this Topology topology)
        {
            return topology switch
            {
                Topology.MinimumExclusiveMaximumInclusive or Topology.Exclusive => topology,
                Topology.MinimumInclusiveMaximumExclusive => Topology.Exclusive,
                Topology.Inclusive => Topology.MinimumExclusiveMaximumInclusive,
                _ => throw new System.InvalidOperationException(),
            };
        }

        public static Topology OpenMaximum(this Topology topology)
        {
            return topology switch
            {
                Topology.MinimumInclusiveMaximumExclusive or Topology.Exclusive => topology,
                Topology.MinimumExclusiveMaximumInclusive => Topology.Exclusive,
                Topology.Inclusive => Topology.MinimumInclusiveMaximumExclusive,
                _ => throw new System.InvalidOperationException(),
            };
        }

    }
}