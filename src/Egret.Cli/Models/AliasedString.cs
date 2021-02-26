using LanguageExt;
using MoreLinq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using static LanguageExt.Prelude;

namespace Egret.Cli.Models
{
    public record AliasedString : IEquatable<AliasedString>, IEnumerable<string>
    {
        public AliasedString(Seq<string> aliases)
        {
            if (aliases.Any(isnull))
            {
                throw new ArgumentNullException(nameof(aliases), "Null is not an allowed alias");
            }
            Aliases = aliases;
        }

        public AliasedString(params string[] aliases) : this(aliases.ToSeq())
        {
        }

        // this overload used for deserialization
        public AliasedString(IEnumerable<string> aliases) : this(aliases.ToSeq())
        {
        }

        public Seq<string> Aliases { get; init; }

        public AliasedString With(params string[] head)
        {
            return new AliasedString(head.ToSeq() + Aliases);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Aliases.GetEnumerator();
        }

        public Option<string> Match(string other, StringComparison comparison = default)
        {
            if (other is null)
            {
                return None;
            }

            return Aliases.Find(x => x.Equals(other, comparison));
        }
        public Option<(string MatchedAlias, string MatchedOther)> MatchAny(IEnumerable<string> other, StringComparison comparison = default)
        {
            if (!other.Any())
            {
                return None;
            }

            return Aliases.Cartesian(other, ValueTuple.Create).Find(pair => pair.Item1.Equals(pair.Item2, comparison));
        }

        public override string ToString()
        {
            return Aliases.JoinIntoSetNotation();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static bool operator ==(string left, AliasedString right)
        {
            return right.Match(left).IsSome;
        }

        public static bool operator !=(string left, AliasedString right)
        {
            return right.Match(left).IsNone;
        }
    }
}