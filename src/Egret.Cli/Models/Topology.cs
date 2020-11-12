using System;

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

        Open = 0b0_1,
        LeftClosedRightOpen = 0b0_0,
        LeftOpenRightClosed = 0b1_1,
        Closed = 0b1_0,


        Exclusive = Open,
        MinimumInclusiveMaximumExclusive = LeftClosedRightOpen,
        MinimumExclusiveMaximumInclusive = LeftOpenRightClosed,
        Inclusive = Closed,

        Default = LeftClosedRightOpen,
    }




}