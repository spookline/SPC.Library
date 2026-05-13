using System.Collections.Generic;

namespace Spookline.SPC.Common {
    public static class StringExtensions {

        public static string JoinStrings<T>(this T enumerable, string separator = ",") where T : IEnumerable<object> {
            return string.Join(separator, enumerable);
        }

        public static string JoinStringsGroup<T>(
            this T enumerable,
            string separator = ",",
            string start = "[",
            string end = "]"
        ) where T : IEnumerable<object> {
            return start + string.Join(separator, enumerable) + end;
        }

    }
}