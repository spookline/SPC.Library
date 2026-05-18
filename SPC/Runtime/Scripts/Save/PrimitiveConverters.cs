using Dahomey.Cbor.ObjectModel;

namespace Spookline.SPC.Save {
    public static class PrimitiveConverters {

        public static bool TryStr(this CborValue value, out string str, string defaultValue = null) {
            if (value is CborString cborString) {
                str = cborString.Value<string>();
                return true;
            }

            str = defaultValue;
            return false;
        }

        public static string Str(this CborValue value) {
            if (value is CborString cborString) { return cborString.Value<string>(); }

            throw new InvalidCborException<string>(value);
        }

        public static bool TryInt(this CborValue value, out int result, int defaultValue = 0) {
            if (value is CborPositive positive) {
                result = positive.Value<int>();
                return true;
            } else if (value is CborNegative negative) {
                result = negative.Value<int>();
                return true;
            }

            result = defaultValue;
            return false;
        }

        public static int Int(this CborValue value) {
            if (value is CborPositive positive) { return positive.Value<int>(); } else if
                (value is CborNegative negative) { return negative.Value<int>(); }

            throw new InvalidCborException<int>(value);
        }

        public static bool TryFloat(this CborValue value, out float result, float defaultValue = 0f) {
            if (value is CborSingle single) {
                result = single.Value<float>();
                return true;
            } else if (value is CborDouble dbl) {
                result = (float)dbl.Value<double>();
                return true;
            }

            result = defaultValue;
            return false;
        }

        public static float Float(this CborValue value) {
            if (value is CborSingle single) { return single.Value<float>(); } else if (value is CborDouble dbl) {
                return (float)dbl.Value<double>();
            }

            throw new InvalidCborException<float>(value);
        }

        public static bool TryBool(this CborValue value, out bool result, bool defaultValue = false) {
            if (value is CborBoolean boolean) {
                result = boolean.Value<bool>();
                return true;
            }

            result = defaultValue;
            return false;
        }

        public static bool Bool(this CborValue value) {
            if (value is CborBoolean boolean) { return boolean.Value<bool>(); }

            throw new InvalidCborException<bool>(value);
        }

        public static bool TryLong(this CborValue value, out long result, long defaultValue = 0L) {
            if (value is CborPositive positive) {
                result = positive.Value<long>();
                return true;
            } else if (value is CborNegative negative) {
                result = negative.Value<long>();
                return true;
            }

            result = defaultValue;
            return false;
        }

        public static long Long(this CborValue value) {
            if (value is CborPositive positive) { return positive.Value<long>(); } else if
                (value is CborNegative negative) { return negative.Value<long>(); }

            throw new InvalidCborException<long>(value);
        }

        public static bool TryGetArray(this CborValue value, out CborArray result) {
            if (value is CborArray array) {
                result = array;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryGetArray(this CborValue value, out CborArray result, int length) {
            if (value is CborArray array) {
                if (array.Count != length) {
                    result = null;
                    return false;
                }

                result = array;
                return true;
            }

            result = null;
            return false;
        }


        public static bool TryGetObject(this CborValue value, out CborObject result) {
            if (value is CborObject obj) {
                result = obj;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryGetMember(this CborValue value, string name, out CborValue result) {
            if (value is CborObject obj) {
                if (obj.TryGetValue(name, out result)) return true;
            }

            result = null;
            return false;
        }

        public static bool TryGetMember(this CborObject value, string name, out CborValue result) {
            if (value.TryGetValue(name, out result)) return true;
            result = null;
            return false;
        }

        public static bool IsNull(this CborValue value) => value is CborNull or null;

        public static CborValue ToCbor(this string str) => CborValueConvert.ToValue(str);
        public static CborValue ToCbor(this int i) => CborValueConvert.ToValue(i);
        public static CborValue ToCbor(this float f) => CborValueConvert.ToValue(f);
        public static CborValue ToCbor(this bool b) => CborValueConvert.ToValue(b);
        public static CborValue ToCbor(this long l) => CborValueConvert.ToValue(l);

    }
}