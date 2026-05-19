using System;
using System.Runtime.Serialization;
using Dahomey.Cbor.ObjectModel;
using JetBrains.Annotations;
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

        public static CborValue ToCbor(this AffineTransform transform) {
            math.decompose(transform, out var scale, out var rotationValue, out var translation);
            var rotation = rotationValue.value;
            return new CborArray(
                translation.x,
                translation.y,
                translation.z,
                rotation.x,
                rotation.y,
                rotation.z,
                rotation.w,
                scale.x,
                scale.y,
                scale.z
            );
        }

        public static CborValue ToCbor(this RigidTransform transform) {
            var translation = transform.pos;
            var rotation = transform.rot.value;
            return new CborArray(
                translation.x,
                translation.y,
                translation.z,
                rotation.x,
                rotation.y,
                rotation.z,
                rotation.w
            );
        }

        public static bool TryAffineTransform(this CborValue value, out AffineTransform result) {
            result = Unity.Mathematics.AffineTransform.identity;
            if (value.TryGetArray(out var array, 12)) {
                if (!array[0].TryFloat(out var tx)) return false;
                if (!array[1].TryFloat(out var ty)) return false;
                if (!array[2].TryFloat(out var tz)) return false;
                if (!array[3].TryFloat(out var rx)) return false;
                if (!array[4].TryFloat(out var ry)) return false;
                if (!array[5].TryFloat(out var rz)) return false;
                if (!array[6].TryFloat(out var rw)) return false;
                if (!array[7].TryFloat(out var sx)) return false;
                if (!array[8].TryFloat(out var sy)) return false;
                if (!array[9].TryFloat(out var sz)) return false;

                result = new AffineTransform(
                    new float3(tx, ty, tz),
                    new quaternion(rx, ry, rz, rw),
                    new float3(sx, sy, sz)
                );
                return true;
            }

            return false;
        }

        public static AffineTransform AffineTransform(this CborValue value) {
            if (value.TryAffineTransform(out var result)) return result;
            throw new InvalidCborException<AffineTransform>(value);
        }

        public static bool TryRigidTransform(this CborValue value, out RigidTransform result) {
            result = Unity.Mathematics.RigidTransform.identity;
            if (value.TryGetArray(out var array, 7)) {
                if (!array[0].TryFloat(out var tx)) return false;
                if (!array[1].TryFloat(out var ty)) return false;
                if (!array[2].TryFloat(out var tz)) return false;
                if (!array[3].TryFloat(out var rx)) return false;
                if (!array[4].TryFloat(out var ry)) return false;
                if (!array[5].TryFloat(out var rz)) return false;
                if (!array[6].TryFloat(out var rw)) return false;

                result = new RigidTransform(
                    new quaternion(rx, ry, rz, rw),
                    new float3(tx, ty, tz)
                );
                return true;
            }

            return false;
        }

        public static RigidTransform RigidTransform(this CborValue value) {
            if (value.TryRigidTransform(out var result)) return result;
            throw new InvalidCborException<RigidTransform>(value);
        }

        public static bool TryInt2(this CborValue value, out int2 result) {
            result = default;
            if (value.TryGetArray(out var array, 2)) {
                if (!array[0].TryInt(out var x)) return false;
                if (!array[1].TryInt(out var y)) return false;
                result = new int2(x, y);
                return true;
            }

            return false;
        }

        public static int2 Int2(this CborValue value) {
            if (value.TryInt2(out var result)) return result;
            throw new InvalidCborException<int2>(value);
        }

        public static bool TryFloat2(this CborValue value, out float2 result) {
            result = default;
            if (value.TryGetArray(out var array, 2)) {
                if (!array[0].TryFloat(out var x)) return false;
                if (!array[1].TryFloat(out var y)) return false;
                result = new float2(x, y);
                return true;
            }

            return false;
        }

        public static float2 Float2(this CborValue value) {
            if (value.TryFloat2(out var result)) return result;
            throw new InvalidCborException<float2>(value);
        }

        public static bool TryInt3(this CborValue value, out int3 result) {
            result = default;
            if (value.TryGetArray(out var array, 3)) {
                if (!array[0].TryInt(out var x)) return false;
                if (!array[1].TryInt(out var y)) return false;
                if (!array[2].TryInt(out var z)) return false;
                result = new int3(x, y, z);
                return true;
            }

            return false;
        }

        public static int3 Int3(this CborValue value) {
            if (value.TryInt3(out var result)) return result;
            throw new InvalidCborException<int3>(value);
        }

        public static bool TryFloat3(this CborValue value, out float3 result) {
            result = default;
            if (value.TryGetArray(out var array, 3)) {
                if (!array[0].TryFloat(out var x)) return false;
                if (!array[1].TryFloat(out var y)) return false;
                if (!array[2].TryFloat(out var z)) return false;
                result = new float3(x, y, z);
                return true;
            }

            return false;
        }

        public static float3 Float3(this CborValue value) {
            if (value.TryFloat3(out var result)) return result;
            throw new InvalidCborException<float3>(value);
        }

        public static bool TryVector3(this CborValue value, out Vector3 result) {
            result = default;
            if (value.TryGetArray(out var array, 3)) {
                if (!array[0].TryFloat(out var x)) return false;
                if (!array[1].TryFloat(out var y)) return false;
                if (!array[2].TryFloat(out var z)) return false;
                result = new Vector3(x, y, z);
                return true;
            }

            return false;
        }

        public static Vector3 Vector3(this CborValue value) {
            if (value.TryVector3(out var result)) return result;
            throw new InvalidCborException<Vector3>(value);
        }

        public static bool TryQuaternion(this CborValue value, out Quaternion result) {
            result = default;
            if (value.TryGetArray(out var array, 4)) {
                if (!array[0].TryFloat(out var x)) return false;
                if (!array[1].TryFloat(out var y)) return false;
                if (!array[2].TryFloat(out var z)) return false;
                if (!array[3].TryFloat(out var w)) return false;
                result = new Quaternion(x, y, z, w);
                return true;
            }

            return false;
        }

        public static Quaternion Quaternion(this CborValue value) {
            if (value.TryQuaternion(out var result)) return result;
            throw new InvalidCborException<Quaternion>(value);
        }

    }

    public abstract class CborException : System.Exception {

        protected CborException() { }
        protected CborException(string message) : base(message) { }
        protected CborException(string message, Exception innerException) : base(message, innerException) { }

    }

    public class InvalidCborException<T> : CborException {

        public CborValue offending;

        public InvalidCborException(CborValue offending) : base(
            $"CborValue of type '{offending?.Type}' cannot be converted to {typeof(T).Name}."
        ) {
            this.offending = offending;
        }

        public InvalidCborException(CborValue offending, System.Exception inner) : base(
            $"CborValue of type '{offending?.Type}' cannot be converted to {typeof(T).Name}.",
            inner
        ) {
            this.offending = offending;
        }

        public InvalidCborException(CborValue offending, string message) : base(message) {
            this.offending = offending;
        }

        public InvalidCborException(CborValue offending, string message, System.Exception inner) :
            base(message, inner) {
            this.offending = offending;
        }

    }

    public class MissingCborMemberException : CborException {

        public CborValue offending;
        public string member;

        public MissingCborMemberException(CborValue offending, string member) : base(
            $"CborValue does not contain required member '{member}'."
        ) {
            this.offending = offending;
            this.member = member;
        }

    }
}