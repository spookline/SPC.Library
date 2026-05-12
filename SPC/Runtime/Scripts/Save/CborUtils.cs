using System;
using Dahomey.Cbor.ObjectModel;

namespace Spookline.SPC.Save {
    public static class CborUtils {

        public static T ValueOrDefault<T>(this CborValue value, T defaultValue) {
            try {
                return value.Value<T>();
            } catch {
                return defaultValue;
            }
        }
        
        public static T MemberValue<T>(this CborValue cborValue, string key, T defaultValue = default) {
            if (cborValue is not CborObject obj) {
                throw new ArgumentException("CborValue must be a CborObject to access members.");
            }
            return obj.TryGetValue(key, out var value) ? ValueOrDefault(value, defaultValue) : defaultValue;
        }
    }
}