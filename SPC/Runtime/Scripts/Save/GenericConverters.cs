using System;
using System.Collections.Generic;
using Dahomey.Cbor.ObjectModel;

namespace Spookline.SPC.Save {
    public static class GenericConverters {

        public static void Collection<T>(this CborValue value, ICollection<T> collection, Func<CborValue, T> converter) {
            if (value.TryGetArray(out var array)) {
                foreach (var item in array) { collection.Add(converter(item)); }
                return;
            }

            throw new InvalidCborException<ICollection<T>>(value);
        }

        public static List<T> List<T>(this CborValue value, Func<CborValue, T> converter) {
            if (value.TryGetArray(out var array)) {
                var list = new List<T>(array.Count);
                foreach (var item in array) { list.Add(converter(item)); }
                return list;
            }

            throw new InvalidCborException<List<T>>(value);
        }

        public static CborArray ToCbor<T>(this ICollection<T> list, Func<T, CborValue> converter) {
            var array = new CborArray(list.Count);
            foreach (var item in list) { array.Add(converter(item)); }
            return array;
        }

    }
}