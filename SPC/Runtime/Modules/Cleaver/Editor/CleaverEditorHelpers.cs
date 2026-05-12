using Spookline.SPC.Geometry;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Spookline.SPC.Cleaver.Editor {
    public class CleaverEditorHelpers {

        public static void DrawObbWithLabel(OrientedBoxQuery box, string label, Color wireColor) {
            // Draw OBB using the axis information from OrientedBoxQuery
            var center = new Vector3(box.center.x, box.center.y, box.center.z);
            var halfSize = new Vector3(box.halfExtent.x, box.halfExtent.y, box.halfExtent.z);

            // Get rotation from axes
            var axisX = new Vector3(box.axisX.x, box.axisX.y, box.axisX.z);
            var axisY = new Vector3(box.axisY.x, box.axisY.y, box.axisY.z);
            var axisZ = new Vector3(box.axisZ.x, box.axisZ.y, box.axisZ.z);

            // Draw wireframe
            Handles.color = wireColor;
            DrawObbWireframe(center, halfSize, axisX, axisY, axisZ);

            DrawLabel(label, center + Vector3.up * 0.2f, wireColor);
        }

        public static void DrawObbWireframe(
            float3 center,
            float3 halfSize,
            float3 axisX,
            float3 axisY,
            float3 axisZ
        ) {
            var right = axisX * halfSize.x;
            var up = axisY * halfSize.y;
            var forward = axisZ * halfSize.z;

            var c000 = center - right - up - forward;
            var c100 = center + right - up - forward;
            var c010 = center - right + up - forward;
            var c110 = center + right + up - forward;
            var c001 = center - right - up + forward;
            var c101 = center + right - up + forward;
            var c011 = center - right + up + forward;
            var c111 = center + right + up + forward;

            // Bottom face
            Handles.DrawLine(c000, c100);
            Handles.DrawLine(c100, c110);
            Handles.DrawLine(c110, c010);
            Handles.DrawLine(c010, c000);

            // Top face
            Handles.DrawLine(c001, c101);
            Handles.DrawLine(c101, c111);
            Handles.DrawLine(c111, c011);
            Handles.DrawLine(c011, c001);

            // Vertical edges
            Handles.DrawLine(c000, c001);
            Handles.DrawLine(c100, c101);
            Handles.DrawLine(c110, c111);
            Handles.DrawLine(c010, c011);
        }

        public static void DrawLabel(string text, float3 worldPos, Color color) {
            Handles.BeginGUI();
            var screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            var bgWidth = 40f;
            var bgHeight = 16f;
            var bgStartX = screenPos.x - bgWidth / 2f;
            var bgRect = new Rect(bgStartX, screenPos.y, bgWidth, bgHeight);

            GUI.backgroundColor = Color.black;
            GUI.Box(bgRect, "");
            GUI.color = color;
            GUI.Label(
                bgRect,
                text,
                new GUIStyle(GUI.skin.label) {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                }
            );
            GUI.backgroundColor = Color.white;
            Handles.EndGUI();
        }

    }
}