using System;
using System.Collections.Generic;
using Dahomey.Cbor.ObjectModel;

namespace Spookline.SPC.Save {
    public static class GenericConverters {

        public static void Collection<T>(
            this CborValue value,
            ICollection<T> collection,
            Func<CborValue, T> converter
        ) {
            if (!value.TryGetArray(out var array)) throw new InvalidCborException<ICollection<T>>(value);
            array.CborScopeAction<ICollection<T>>(Produce);
            return;

            void Produce() {
                foreach (var item in array) {
                    var converted = item.CborScopeProducer(converter);
                    collection.Add(converted);
                }
            }
        }

        public static List<T> List<T>(this CborValue value, Func<CborValue, T> converter) {
            if (!value.TryGetArray(out var array)) throw new InvalidCborException<List<T>>(value);
            return array.CborScopeProducer(ProduceList);

            List<T> ProduceList() {
                var list = new List<T>(array.Count);
                foreach (var item in array) {
                    var converted = item.CborScopeProducer(converter);
                    list.Add(converted);
                }

                return list;
            }
        }

        public static CborArray ToCbor<T>(this ICollection<T> list, Func<T, CborValue> converter) {
            var array = new CborArray {
                Capacity = list.Count
            };
            foreach (var item in list) { array.Add(converter(item)); }

            return array;
        }

    }
}