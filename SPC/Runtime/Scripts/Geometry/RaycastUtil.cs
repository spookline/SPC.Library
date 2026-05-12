using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spookline.SPC.Geometry {
    public static class RaycastUtil {

        /// <summary>
        /// Visualizes a cone in the Unity Editor using Gizmos.
        /// </summary>
        /// <param name="origin">The starting position of the cone.</param>
        /// <param name="direction">The direction in which the cone is facing.</param>
        /// <param name="coneCastRange">The range or distance of the cone.</param>
        /// <param name="coneCastAngle">The angle of the cone in degrees.</param>
        /// <param name="numSegments">The number of segments used to approximate the circular base of the cone. Defaults to 16.</param>
        public static void VisualizeCone(Vector3 origin, Vector3 direction, float coneCastRange, float coneCastAngle,
            int numSegments = 16) {
            var angleRad = coneCastAngle * Mathf.Deg2Rad;
            Gizmos.DrawRay(origin, direction * coneCastRange);
            for (var i = 0; i < numSegments; i++) {
                var angle = (i / (float)numSegments) * Mathf.PI * 2f;
                var right = Vector3.Cross(direction, Vector3.up).normalized;
                if (right.sqrMagnitude < 0.01f) right = Vector3.Cross(direction, Vector3.forward).normalized;
                var up = Vector3.Cross(right, direction).normalized;
                var offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * Mathf.Tan(angleRad);
                var coneDirection = (direction + offset).normalized;
                Gizmos.DrawRay(origin, coneDirection * coneCastRange);
            }
        }

        /// <summary>
        /// Performs a cone-shaped overlap check and returns a list of colliders within the specified range and angle.
        /// </summary>
        /// <param name="origin">The starting position of the cone.</param>
        /// <param name="forward">The forward direction of the cone.</param>
        /// <param name="range">The range or distance of the cone.</param>
        /// <param name="angle">The angle of the cone in degrees.</param>
        /// <param name="mask">The layer mask used to filter specific objects.</param>
        /// <param name="queryTriggerInteraction">Specifies whether to include or ignore trigger colliders during the overlap check.</param>
        /// <return>Returns a list of colliders that fall within the cone's range and angle.</return>
        public static List<Collider> OverlapCone(
            Vector3 origin,
            Vector3 forward,
            float range,
            float angle,
            LayerMask mask,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide) {
            var results = new List<Collider>();
            var colliders = Physics.OverlapSphere(origin, range, mask, queryTriggerInteraction);
            if (colliders == null || colliders.Length == 0) return results;
            var maxDot = Mathf.Cos(angle * Mathf.Deg2Rad);
            var forwardNormalized = forward.normalized;
            results.AddRange(from col in colliders
                let directionToTarget = (col.transform.position - origin).normalized
                let dot = Vector3.Dot(forwardNormalized, directionToTarget)
                where !(dot < maxDot)
                select col);
            return results.Distinct().ToList();
        }

        /// <summary>
        /// Finds all colliders within a cone-shaped area based on the specified origin, forward direction, range, and angle.
        /// </summary>
        /// <param name="origin">The origin point of the cone.</param>
        /// <param name="forward">The forward direction of the cone.</param>
        /// <param name="range">The maximum range or distance of the cone.</param>
        /// <param name="angle">The angle of the cone in degrees.</param>
        /// <param name="mask">The LayerMask used to filter which colliders to consider.</param>
        /// <param name="queryTriggerInteraction">Specifies whether Trigger colliders are included in the query.</param>
        /// <returns>A list of colliders that are within the cone-shaped area.</returns>
        public static List<T> OverlapCone<T>(
            Vector3 origin,
            Vector3 forward,
            float range,
            float angle,
            LayerMask mask,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide)
            where T : class {
            var results = new List<T>();
            var colliders = OverlapCone(origin, forward, range, angle, mask, queryTriggerInteraction);
            foreach (var col in colliders) {
                if (col.TryGetComponent<T>(out var component)) {
                    results.Add(component);
                    continue;
                }

                var parentComponent = col.GetComponentInParent<T>();
                if (parentComponent != null) {
                    results.Add(parentComponent);
                }
            }

            return results.Distinct().ToList();
        }

    }
}