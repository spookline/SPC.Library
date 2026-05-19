using System.Collections.Generic;
using Dahomey.Cbor.ObjectModel;
using UnityEngine;

namespace Spookline.SPC.Save {
    public interface IVersionAware {

        public int Version { get; set; }
        public Dictionary<string, int> Extensions { get; set; }

    }

    public interface IDataWriter {

        public CborObject Obj { get; }

    }

    public interface IDataReader {

        public CborObject Obj { get; }

    }

    public struct DataReader : IDataReader {

        public CborObject Obj { get; }

        public DataReader(CborObject backingObject) {
            Obj = backingObject;
        }

    }

    public struct DataWriter : IDataWriter, IDataReader {

        public CborObject Obj { get; }

        public DataWriter(CborObject backingObject) {
            Obj = backingObject;
        }

    }

    public static class ReaderExtensions {

        public static CborValue ReadData(this IDataReader reader, string key) {
            return reader.Obj.TryGetValue(key, out var value) ? value : null;
        }

        public static bool TryReadData(this IDataReader reader, string key, out CborValue value) {
            if (reader.Obj.TryGetValue(key, out var cborValue)) {
                value = cborValue;
                return true;
            }

            value = null;
            return false;
        }

        public static bool TryReadData<T>(this IDataReader reader, string key, out T value) {
            if (reader.Obj.TryGetValue(key, out var cborValue)) {
                try {
                    value = cborValue.Value<T>();
                    return true;
                } catch {
                    value = default;
                    return false;
                }
            }

            value = default;
            return false;
        }

        public static CborValue Member(this IDataReader reader, string key) {
            if (reader.Obj.TryGetMember(key, out var value)) { return value; }

            throw new MissingCborMemberException(reader.Obj, key);
        }

        public static CborValue MemberNullable(this IDataReader reader, string key) {
            if (reader.Obj.TryGetMember(key, out var value)) { return value is CborNull ? null : value; }

            throw new MissingCborMemberException(reader.Obj, key);
        }

        public static CborValue MemberOptional(this IDataReader reader, string key) {
            if (reader.Obj.TryGetMember(key, out var value)) { return value is CborNull ? null : value; }

            return null;
        }

    }

    public static class WriterExtensions {

        public static void WriteData(this IDataWriter writer, string key, CborValue value) {
            if (value == null) {
                Debug.LogWarning($"Attempted to write null value for key '{key}'");
                return;
            }

            writer.Obj[key] = value;
        }

    }
}