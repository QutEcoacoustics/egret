using LanguageExt;
using System;
using System.Linq;
using System.Collections.Generic;
using static LanguageExt.Prelude;
using MoreLinq;

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

        public static IEnumerable<(A, B, V)> Flatten<A, B, V>(this Map<A, Map<B, V>> self)
        {
            return self.SelectMany(
                (itemA) => itemA.Value.Select(
                    (itemB) => (itemA.Key, itemB.Key, itemB.Value)
                )
            );
        }

        public static IEnumerable<(A, B, C, V)> Flatten<A, B, C, V>(this Map<A, Map<B, Map<C, V>>> self)
        {
            return self.SelectMany(
                (itemA) => itemA.Value.SelectMany(
                    (itemB) => itemB.Value.Select(
                        (itemC) => (itemA.Key, itemB.Key, itemC.Key, itemC.Value)
                    )
                )
            );
        }
    }
}