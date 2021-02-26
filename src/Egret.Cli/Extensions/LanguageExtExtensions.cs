using LanguageExt;
using System;
using static LanguageExt.Prelude;

namespace Egret.Cli.Extensions
{
    public static class LanguageExtExtensions
    {
        public static Validation<F, S> Or<F, S>(this Validation<F, S> first, Validation<F, S> second)
        {
            if (first.IsSuccess)
            {
                return first;
            }

            if (second.IsSuccess)
            {
                return second;
            }

            return first | second;
        }


    }
}