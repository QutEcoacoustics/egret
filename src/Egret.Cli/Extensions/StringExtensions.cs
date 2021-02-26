using LanguageExt;
using System.Collections.Generic;
using System.Linq;
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
        public static string JoinMoreThanOneIntoSetNotation(this IEnumerable<string> items)
        {
            if (items.Skip(1).Any())
            {
                return "{" + string.Join(", ", items) + "}";
            }

            return $"{items.Single()}";
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
    }
}