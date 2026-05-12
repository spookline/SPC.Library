using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace Spookline.SPC.Common {
    public static class RandomExtensions {
        public static void ShuffleInPlace<T>(this List<T> list) {
            var count = list.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i) {
                var r = Random.Range(i, count);
                (list[i], list[r]) = (list[r], list[i]);
            }
        }

        public static List<T> Shuffled<T>(this IEnumerable<T> enumerable) {
            var shuffled = enumerable.ToList();
            shuffled.ShuffleInPlace();
            return shuffled;
        }

        public static T PickRandom<T>(this IEnumerable<T> enumerable) {
            var list = enumerable.ToList();
            return list.Count == 0 ? default : list[Random.Range(0, list.Count)];
        }
        
        public static T PickRandomWeighted<T>(this IEnumerable<T> enumerable, Func<T, int> weightFunc) {
            var list = enumerable.ToList();
            var sum = list.Sum(weightFunc);
            if (sum <= 0) return default;
            var random = Random.Range(0, sum);
            var currentWeight = 0;
            foreach (var item in list) {
                currentWeight += weightFunc(item);
                if (random < currentWeight) return item;
            }
            return list.LastOrDefault();
        }
    }

    public static class ListExtensions {
        public static bool RemoveFirst<T>(this List<T> list, Predicate<T> predicate) {
            var index = list.FindIndex(predicate);
            if (index == -1) return false;
            list.RemoveAt(index);
            return true;
        }
    }
}