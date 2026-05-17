using System;
using System.Globalization;
using UnityEngine;

namespace Spookline.SPC.Common {
    public static class CachedNumberFormat {

        private const int _twoDecMin = -256;
        private const int _twoDecMax = 256;
        private const int _oneDecMin = -1024;
        private const int _oneDecMax = 1024;
        private const int _intMin = -1024;
        private const int _intMax = 4096;

        private static string[] _twoDecimals; // -256.00 .. 256.00
        private static string[] _oneDecimal; // -1024.0 .. 1024.0
        private static string[] _integers; // -1024 .. 4096

        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        public static void Init() {
            if (IsInitialized) return;

            var twoCount = (_twoDecMax - _twoDecMin) * 100 + 1;
            var oneCount = (_oneDecMax - _oneDecMin) * 10 + 1;
            var intCount = _intMax - _intMin + 1;

            _twoDecimals = new string[twoCount];
            _oneDecimal = new string[oneCount];
            _integers = new string[intCount];

            var culture = CultureInfo.InvariantCulture;
            var byteCount = 0L;

            const int stringOverhead = 24 + 8; // String and reference overhead
            const int charSize = sizeof(char) * 2; // Conservative using UTF-16

            for (var i = 0; i < twoCount; i++) {
                var v = _twoDecMin + i / 100f;
                var str = v.ToString("0.00", culture);
                _twoDecimals[i] = str;
                byteCount += str.Length * charSize;
                byteCount += stringOverhead;
            }

            for (var i = 0; i < oneCount; i++) {
                var v = _oneDecMin + i / 10f;
                var str = v.ToString("0.0", culture);
                _oneDecimal[i] = str;
                byteCount += str.Length * charSize;
                byteCount += stringOverhead;
            }

            for (var i = 0; i < intCount; i++) {
                var v = _intMin + i;
                var str = v.ToString(culture);
                _integers[i] = str;
                byteCount += str.Length * charSize;
                byteCount += stringOverhead;
            }

            _initialized = true;
            Debug.Log($"CachedNumberText initialized, size ceiling is conservatively {byteCount / 1024.0 / 1024.0:F2}MiB");
        }


        public static void Clear() {
            _initialized = false;
            _twoDecimals = null;
            _oneDecimal = null;
            _integers = null;
        }

        public static string Format(float value, int decimals, bool fallbackPrecision = true) {
            decimals = Math.Clamp(decimals, 0, 2);

            if (IsInitialized) {
                if (decimals == 2 && TryTwoDecimals(value, out var s)) return s;
                if ((fallbackPrecision || decimals == 1) && decimals >= 1 && TryOneDecimal(value, out s)) return s;
                if ((fallbackPrecision || decimals == 0) && TryInteger(value, out s)) return s;
            }

            // Outside all cache ranges. This allocates.
            return decimals switch {
                2 => value.ToString("0.00", CultureInfo.InvariantCulture),
                1 => value.ToString("0.0", CultureInfo.InvariantCulture),
                _ => value.ToString("0", CultureInfo.InvariantCulture)
            };
        }

        public static string Format(int value, bool fallbackPrecision = true) {
            return Format(value, 0, fallbackPrecision);
        }

        public static string FormatNonAlloc(this float value, int decimals = 1, bool fallbackPrecision = true) {
            return Format(value, decimals, fallbackPrecision);
        }

        public static string FormatNonAlloc(this int value, bool fallbackPrecision = true) {
            return Format(value, 0, fallbackPrecision);
        }

        private static bool TryTwoDecimals(float value, out string text) {
            var scaled = RoundToInt(value * 100f);
            var index = scaled - _twoDecMin * 100;

            if ((uint)index < (uint)_twoDecimals.Length) {
                text = _twoDecimals[index];
                return true;
            }

            text = null;
            return false;
        }

        private static bool TryOneDecimal(float value, out string text) {
            var scaled = RoundToInt(value * 10f);
            var index = scaled - _oneDecMin * 10;

            if ((uint)index < (uint)_oneDecimal.Length) {
                text = _oneDecimal[index];
                return true;
            }

            text = null;
            return false;
        }

        private static bool TryInteger(float value, out string text) {
            var rounded = RoundToInt(value);
            var index = rounded - _intMin;

            if ((uint)index < (uint)_integers.Length) {
                text = _integers[index];
                return true;
            }

            text = null;
            return false;
        }

        private static int RoundToInt(float value) {
            return (int)MathF.Round(value, MidpointRounding.AwayFromZero);
        }

    }
}