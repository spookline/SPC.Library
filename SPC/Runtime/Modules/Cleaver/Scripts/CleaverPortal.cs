using System;
using System.Collections;
using System.Linq;
using Sirenix.OdinInspector;
using Spookline.SPC.Common;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Ext;
using Spookline.SPC.Geometry;
using Unity.AI.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [AddComponentMenu("Cleaver/Portal")]
    public class CleaverPortal : SpookBehaviour<CleaverPortal> {

        [HorizontalGroup("Connections")]
        [HideLabel]
        [SuffixLabel("From")]
        public CleaverSection from;

        [HorizontalGroup("Connections")]
        [HideLabel]
        [SuffixLabel("To")]
        public CleaverSection to;

        [FormerlySerializedAs("region")]
        public CleaverProxyGroup proxyGroup;

        [HideInInspector]
        public bool open = true;

        public float3 size;
        public float spacing = 0.5f;

        [ValueDropdown("LinkAreaCandidates")]
        public int linkArea;
        [NonSerialized]
        public bool implicitLink;
        [NonSerialized]
        public bool implicitObstacle;

        [NonSerialized]
        public NavMeshLink link;

        [NonSerialized]
        public NavMeshObstacle obstacle;

        [ShowInInspector]
        public bool Open {
            get => open;
            set => SetOpen(value);
        }

        private static IEnumerable LinkAreaCandidates {
            get {
                var list = new ValueDropdownList<int>();
                foreach (var areaName in NavMesh.GetAreaNames()) list.Add(areaName, NavMesh.GetAreaFromName(areaName));
                return list;
            }
        }

        public ulong IdFrom { get; private set; }
        public ulong IdTo { get; private set; }

        private void Awake() {
            IdFrom = IdGenerator.NextId();
            IdTo = IdGenerator.NextId();
            link = this.GetOrAddComponent<NavMeshLink>(onCreated: comp => { implicitLink = true; });
            obstacle = this.GetOrAddComponent<NavMeshObstacle>(onCreated: comp => { implicitObstacle = true; });
            UpdateNavmeshComponents();

            On<CleaverEnvironmentRebuiltEvt>().Do(OnEnvironmentRebuilt);
            On<GizmoEvt>().Do(OnGizmos);
        }


        [Title("Helpers")]
        [Button("Connect Automatically")]
        public void DetectSections() {
            float3 position = transform.position;
            var objects = FindObjectsByType<CleaverSection>();

            var overlapping = objects
                .Where(x => x.Contains(position, 2f))
                .ToList();
            if (overlapping.Count >= 2) {
                from = overlapping[0];
                to = overlapping[1];
            } else {
                from = null;
                to = null;
            }
        }

        private void OnGizmos(ref GizmoEvt args) {
            if (!args.HasFlag("cleaver_portals") && args.type == GizmoType.Runtime) return;
            var box = OrientedBox.FromWorldGroundAlignedBox(transform.position, size, transform.rotation);
            if (args.DrawingPass(out var draw)) {
                if (from && to) {
                    var color = open ? Color.green : Color.red;
                    using (draw.Scope(color)) { draw.Line(from.transform.position, to.transform.position); }
                }
            }

            if (args.Cull(box) && args.type == GizmoType.Runtime) return;
            if (args.DrawingPass(out draw)) {
                var boxColor = Color.yellow;
                using (draw.Scope(boxColor)) draw.OrientedBox(box);

                boxColor.a = open ? 0.2f : 0.8f;
                using (draw.Scope(boxColor)) { draw.OrientedBox(box, false); }
            }

            if (args.WorldOverlayPass(out var world)) {

                var env = CleaverEnvironment.Instance;
                if (!env.portalLookup.TryGetValue(IdTo, out var toIdx)) toIdx = -1;
                if (!env.portalLookup.TryGetValue(IdFrom, out var fromIdx)) fromIdx = -1;

                var id = IdTo;
                if (IdTo == 0) id = IdFrom;
                world.Box(id, "Portal", transform.position)
                    .Field("From", fromIdx)
                    .Field("To", toIdx)
                    .Field("Open", open ? "Yes" : "No", color: open ? Color.green : Color.red);
            }

        }

        private void OnDrawGizmosSelected() {
            var evt = GizmoEvt.EditorGizmosSelected;
            OnGizmos(ref evt);
        }

        public void UpdateNavmeshComponents() {
            if (obstacle && implicitObstacle) {
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = new float3(0, size.y * 0.5f, 0);
                obstacle.size = size;
                obstacle.carving = true;
            }

            if (link && implicitLink) {
                link.startPoint = new Vector3(0, 0, spacing + size.z);
                link.endPoint = new Vector3(0, 0, -spacing - size.z);
            }

            if (link) link.enabled = !open;
            if (obstacle) obstacle.enabled = !open;
        }


        public void SetOpen(bool isOpen) {
            if (isOpen == open) return;
            // TODO: Notify environment
            open = isOpen;
            UpdateNavmeshComponents();
        }

        private void OnEnvironmentRebuilt(ref CleaverEnvironmentRebuiltEvt args) {
            UpdateNavmeshComponents();
            link.UpdateLink();
        }

    }
}