using Dahomey.Cbor.ObjectModel;
using Unity.Mathematics;
using UnityEngine;

namespace Spookline.SPC.Save {
    public static class VectorConverters {

        public static CborValue ToCbor(this int2 vector) {
            return new CborArray(vector.x, vector.y);
        }

        public static CborValue ToCbor(this float2 vector) {
            return new CborArray(vector.x, vector.y);
        }

        public static CborValue ToCbor(this int3 vector) {
            return new CborArray(vector.x, vector.y, vector.z);
        }

        public static CborValue ToCbor(this float3 vector) {
            return new CborArray(vector.x, vector.y, vector.z);
        }

        public static CborValue ToCbor(this Vector3 vector) {
            return new CborArray(vector.x, vector.y, vector.z);
        }

        public static CborValue ToCbor(this Quaternion quaternion) {
            return new CborArray(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }

        public static int2 ToInt2(this CborValue value) {
            if (value is CborArray { Count: 2 } array) {
                return new int2(array[0].Value<int>(), array[1].Value<int>());
            }

            throw new System.InvalidCastException("CborValue is not a valid int2 representation.");
        }

        public static float2 ToFloat2(this CborValue value) {
            if (value is CborArray { Count: 2 } array) {
                return new float2(array[0].Value<float>(), array[1].Value<float>());
            }

            throw new System.InvalidCastException("CborValue is not a valid float2 representation.");
        }

        public static int3 ToInt3(this CborValue value) {
            if (value is CborArray { Count: 3 } array) {
                return new int3(array[0].Value<int>(), array[1].Value<int>(), array[2].Value<int>());
            }

            throw new System.InvalidCastException("CborValue is not a valid int3 representation.");
        }

        public static float3 ToFloat3(this CborValue value) {
            if (value is CborArray { Count: 3 } array) {
                return new float3(array[0].Value<float>(), array[1].Value<float>(), array[2].Value<float>());
            }

            throw new System.InvalidCastException("CborValue is not a valid float3 representation.");
        }

        public static Vector3 ToVector3(this CborValue value) {
            if (value is CborArray { Count: 3 } array) {
                return new Vector3(
                    array[0].Value<float>(),
                    array[1].Value<float>(),
                    array[2].Value<float>()
                );
            }

            throw new System.InvalidCastException("CborValue is not a valid Vector3 representation.");
        }

        public static Quaternion ToQuaternion(this CborValue value) {
            if (value is CborArray { Count: 4 } array) {
                return new Quaternion(
                    array[0].Value<float>(),
                    array[1].Value<float>(),
                    array[2].Value<float>(),
                    array[3].Value<float>()
                );
            }

            throw new System.InvalidCastException("CborValue is not a valid Quaternion representation.");
        }

    }
}