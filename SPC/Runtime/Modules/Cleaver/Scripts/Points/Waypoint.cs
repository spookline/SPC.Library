using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Geometry;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace Spookline.SPC.Cleaver.Points {
    public class Waypoint : CleaverPoint<Waypoint.Authoring> {

        public const string Name = "Waypoint";

        [Serializable, TypeRegistryItem(Name, Icon = SdfIconType.PinMapFill, Priority = 100)]
        public class Authoring : EditablePoint {

            public override string TypeName => Name;

            [OdinSerialize, HideLabel, InlineProperty, PolymorphicDrawerSettings(ShowBaseType = false)]
            public WaypointType type = new GeneralWaypoint();

            public override CleaverPoint Instantiate(AffineTransform transform) {
                type ??= GeneralWaypoint.Instance;
                return new Waypoint(this, transform.TransformPoint(position));
            }

            public override void CopyFrom(EditablePoint other) {
                if (other is not Authoring o) return;
                position = o.position;
                type = o.type;
            }

            public override EditablePoint Clone() {
                return new Authoring {
                    position = position,
                    type = type ?? GeneralWaypoint.Instance
                };
            }

            public override void DrawEditor(AffineTransform transform, IDrawingAPI draw) {
                var wsPoint = transform.TransformPoint(position);
                var color = type?.Color ?? Color.cyan;
                using (draw.Scope(color)) { draw.Sphere(wsPoint, 0.2f); }
            }

            public override void DrawOverlayGUI() {
                position = Gui.Vector3("Position", position);
            }

            public override void DrawHandles(AffineTransform transform) {
                Handles.Position(transform, this);
            }

        }

        public Vector3 Position { get; protected set; }
        public WaypointType Type => source.type;
        public byte Tag { get; private set; }

        public Waypoint(Authoring source, Vector3 position) : base(source) {
            Position = position;
        }

        public override void Initialize(CleaverSection section) {
            base.Initialize(section);
            Tag = Type.Tag;
        }

        public override void Rebuild(CleaverSection section) {
            base.Rebuild(section);
            if (NavMesh.SamplePosition(Position, out var hit, 1f, NavMesh.AllAreas)) { Position = hit.position; }
        }

        public override void Gizmos(ref GizmoEvt evt) {
            base.Gizmos(ref evt);
            if (!evt.HasFlag("cleaver_navmesh")) return;

            if (evt.DrawingPass(out var draw)) {
                using (draw.Scope(source.type.Color)) { draw.Sphere(Position, 0.1f); }
            }
        }
    }

    [Serializable]
    public abstract class WaypointType {

        public virtual Color Color => Color.cyan;
        public abstract byte Tag { get; }

        public override bool Equals(object obj) => obj is WaypointType other && Equals(other);
        protected bool Equals(WaypointType other) => GetType() == other.GetType();

        public override int GetHashCode() => GetType().GetHashCode();

    }

    [Serializable, TypeRegistryItem("General")]
    public class GeneralWaypoint : WaypointType {

        public static GeneralWaypoint Instance = new();

        public override byte Tag => 0;

    }
}