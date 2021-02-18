using LanguageExt;
using System.Collections.Generic;
using System.Text;
using static LanguageExt.Prelude;

namespace System
{
    public static class StringExtensions
    {
        public static string JoinWithComma(this IEnumerable<string> items)
        {
            return string.Join(", ", items);
        }

        public static string JoinIntoSetNotation(this IEnumerable<string> items)
        {
            return "{" + string.Join(", ", items) + "}";
        }


        public static string Join(this IEnumerable<string> items, string separator)
        {
            return string.Join(separator, items);
        }

        public static string JoinWithoutGap(this IEnumerable<string> items)
        {
            return string.Join(string.Empty, items);
        }

        public static string Join(this IEnumerable<string> items, string separator, string wrapper)
        {
            var builder = new StringBuilder();
            foreach (var item in items)
            {
                builder.Append(wrapper);
                builder.Append(item);
                builder.Append(wrapper);
                builder.Append(separator);
            }

            // remove the last separator which is at the end of the list
            builder.Remove(builder.Length - 1, 1);

            return builder.ToString();
        }

        public static string NormalizeBlank(this string @string)
        {
            return string.IsNullOrWhiteSpace(@string) ? null : @string;
        }

        public static Option<(string Firstmatch, string SecondMatch)> MatchThroughAliases(
            this IEnumerable<string> aliases,
            string first,
            string second,
            StringComparison comparison
            )
        {
            if (first.Equals(second, comparison))
            {
                return (first, second);
            }

            // otherwise look for matches through alias array
            Option<string> firstMatch = None;
            Option<string> secondMatch = None;
            foreach (var alias in aliases)
            {
                if (firstMatch.IsNone && first.Equals(alias, comparison))
                {
                    firstMatch = alias;
                }

                if (secondMatch.IsNone && second.Equals(alias, comparison))
                {
                    secondMatch = alias;
                }

                if (firstMatch.IsSome && secondMatch.IsSome)
                {
                    // at some point we've accrued two matches, success!
                    return from f in firstMatch from s in secondMatch select (f, s);
                }
            }

            return None;
        }
    }
}