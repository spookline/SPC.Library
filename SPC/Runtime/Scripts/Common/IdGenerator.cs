using System;

// ReSharper disable InconsistentNaming

namespace Spookline.SPC.Common {
    public static class IdGenerator {
        private const int SequenceBits = 16;
        private const ulong MaxSequence = (1UL << SequenceBits) - 1; // 0xFFFF

        private static readonly DateTime Epoch =
            new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static ulong _lastTimestamp;
        private static ulong _sequence;

        private static readonly object _lock = new();

        public static ulong NextId() {
            lock (_lock) {
                var now = (ulong)(DateTime.UtcNow - Epoch).TotalMilliseconds;
                if (now < _lastTimestamp)
                    throw new InvalidOperationException("Clock moved backwards");

                if (now == _lastTimestamp) {
                    _sequence = (_sequence + 1) & MaxSequence;
                    if (_sequence == 0)
                        do {
                            now = (ulong)(DateTime.UtcNow - Epoch).TotalMilliseconds;
                        } while (now == _lastTimestamp);
                } else {
                    _sequence = 0;
                }

                _lastTimestamp = now;
                return (now << SequenceBits) | _sequence;
            }
        }
    }
}