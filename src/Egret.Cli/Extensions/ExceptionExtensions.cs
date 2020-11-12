using System;
using System.Linq;

namespace Egret.Cli.Extensions
{
    public static class ExceptionExtensions
    {
        public static Exception MakeAggregate<T>(this T exception, params Exception[] others)
        where T : Exception
        {
            var aggregate = others.Prepend(exception).Where(x => x is not null).ToArray();

            return aggregate switch
            {
                { Length: 0 } => null,
                { Length: 1 } => aggregate.First(),
                _ => new AggregateException(aggregate)
            };
        }
    }
}