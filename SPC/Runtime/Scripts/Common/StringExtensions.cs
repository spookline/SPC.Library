using System.Collections.Generic;
using Unity.Collections;

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

    public static class ArrayExtensions {

        public static bool TryGetIndex<T>(this NativeArray<T> array, int i, out T result) where T : struct {
            result = default;
            if (!array.IsCreated) return false;
            if (i < 0 || i >= array.Length) return false;
            result = array[i];
            return true;
        }

    }
}