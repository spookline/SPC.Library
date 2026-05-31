using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sirenix.OdinInspector;
using Spookline.SPC.Debugging;
using Spookline.SPC.Draw;
using Spookline.SPC.Draw.Poly;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;

namespace Spookline.SPC.Cleaver {
    [HideMonoScript]
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Cleaver/Environment")]
    public class CleaverEnvironment : SpookManagerBehaviour<CleaverEnvironment> {

        public bool autoStart = true;
        public bool updateNavmeshAsync;

        public MaskMapping[] maskMappings = Array.Empty<MaskMapping>();

        public float3 worldBounds = new(4096, 512, 4096);
        [NonSerialized]
        public NativeHashMap<ulong, int> groupLookup;
        [NonSerialized]
        public Dictionary<ulong, CleaverProxyGroup> managedGroupLookup = new();
        [NonSerialized]
        public Dictionary<ulong, CleaverPortal> managedPortalLookup = new();
        [NonSerialized]
        public Dictionary<ulong, CleaverProxy> managedProxyLookup = new();

        [NonSerialized]
        public Dictionary<ulong, CleaverSection> managedSectionLookup = new();
        [NonSerialized]
        public NativeHashMap<ulong, int> portalLookup;
        [NonSerialized]
        public NativeArray<CleaverPortalData> portals;
        [NonSerialized]
        public NativeArray<CleaverProxyData> proxies;

        [NonSerialized]
        public NativeArray<CleaverProxyGroupData> proxyGroups;
        [NonSerialized]
        public NativeHashMap<ulong, int> proxyLookup;
        [NonSerialized]
        public NativeArray<float3> samplePoints;

        [NonSerialized]
        public NativeHashMap<ulong, int> sectionLookup;

        [NonSerialized]
        public NativeArray<CleaverSectionData> sections;

        [NonSerialized]
        public NavMeshSurface surface;

        [NonSerialized]
        public NativeArray<CleaverVolumeData> volumes;

        [NonSerialized]
        public NavMeshTriangulation triangulation;
        [NonSerialized]
        public PolyDrawBuffer drawBuffer;

        public uint Version { get; private set; }

        [ShowInInspector]
        [HideInEditorMode]
        [BoxGroup("Statistics", centerLabel: true)]
        [HideLabel]
        public CleaverEnvironmentStats Stats {
            get {
                if (Version == 0) return default;
                try {
                    return new CleaverEnvironmentStats {
                        portalCount = portals.Length,
                        proxyCount = proxies.Length,
                        samplePointCount = samplePoints.Length,
                        volumeCount = volumes.Length,
                        sectionCount = sections.Length,
                        proxyGroupCount = proxyGroups.Length,
                        version = Version
                    };
                } catch (Exception e) {
                    Debug.LogError($"Failed to get statistics: {e}");
                    return default;
                }
            }
        }

        private void Update() {
            if (autoStart && Version == 0) Rebuild();

            if (Version != 0 && Application.isPlaying) {
                // Perform pre-pass to determine state validity
                var updateEvent = new CleaverCheckForUpdateEvt {
                    environment = this
                };
                Evt.RaiseSafe(ref updateEvent);
                if (updateEvent.needsRebuild)
                    Rebuild();
                else if (updateEvent.needsUpdate) IncrementVersion();

                // Perform refresh pass to perform structured state consumption
                var jobs = new NativeList<JobHandle>(Allocator.TempJob);
                try {
                    var refreshEvt = new CleaverBatchedViewerRefreshEvt {
                        environment = this,
                        batch = jobs
                    };
                    Evt.RaiseSafe(ref refreshEvt);
                    if (!jobs.IsEmpty) JobHandle.CompleteAll(jobs.AsArray());
                    jobs.Clear();

                    var raycastEvt = new CleaverBatchedViewerRaycastEvt {
                        environment = this,
                        batch = jobs
                    };
                    Evt.RaiseSafe(ref raycastEvt);
                    if (!jobs.IsEmpty) JobHandle.CompleteAll(jobs.AsArray());
                } finally { jobs.Dispose(); }
            }
        }

        protected override void Awake() {
            base.Awake();
            Debug.Log("Cleaver environment awake", this);

            On<CollectDebugFlagsEvt>().Do(OnCollectDebugFlags);
            On<GizmoEvt>().Do(OnGizmo);
        }

        private void OnGizmo(ref GizmoEvt args) {
            if (!args.HasFlag("cleaver_navmesh")) return;
            if (!drawBuffer.IsCreated) {
                triangulation = NavMesh.CalculateTriangulation();
                var color = Color.magenta;
                color.a = 0.25f;
                drawBuffer = PolyDrawBuffer.From(triangulation.vertices, triangulation.indices, color, true);
            }

            if (args.DrawingPass(out var draw)) {
                using (draw.Scope(PolyDrawBuffer.KeepColor)) draw.MeshBuffer(drawBuffer);
            }
        }

        private void OnCollectDebugFlags(ref CollectDebugFlagsEvt args) {
            args.Add("cleaver_portals", "cleaver_viewers", "cleaver_proxies", "cleaver_sections", "cleaver_navmesh");
        }

        protected override void OnEnable() {
            base.OnEnable();
            AllocateIfNeeded();
            transform.position = float3.zero;

            surface = this.GetOrAddComponent<NavMeshSurface>(
                onCreated: comp => {
                    comp.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                    comp.layerMask = GetMask(ByteMask.World);
                    comp.collectObjects = CollectObjects.Volume;
                    comp.size = worldBounds;
                    comp.buildHeightMesh = true;
                }
            );
        }

        protected override void OnDisable() {
            base.OnDisable();
            DiscardBakedData();

            if (drawBuffer.IsCreated) drawBuffer.Dispose();

            proxyLookup.Dispose();
            portalLookup.Dispose();
            sectionLookup.Dispose();
            groupLookup.Dispose();
        }

        public void QuerySectionsImmediate(
            float3 point,
            byte mask,
            NativeHashSet<ulong> results
        ) {
            const int batchSize = 128;
            results.Clear();

            if (sections.Length <= batchSize) {
                new CleaverSectionPointContainmentJob {
                    sections = sections,
                    volumes = volumes,
                    point = point,
                    queryMask = mask,
                    result = results
                }.Run();
                return;
            }

            QuerySections(point, mask, results).Complete();
        }

        public JobHandle QuerySections(
            float3 point,
            byte mask,
            NativeHashSet<ulong> results,
            JobHandle dependency = default
        ) {
            const int batchSize = 128;
            results.Clear();

            if (sections.Length <= batchSize) {
                return new CleaverSectionPointContainmentJob {
                    sections = sections,
                    volumes = volumes,
                    point = point,
                    queryMask = mask,
                    result = results
                }.Schedule(dependency);
            }

            var queue = new NativeQueue<ulong>(Allocator.TempJob);

            var collectHandle = new CleaverSectionPointContainmentCollectJob {
                sections = sections,
                volumes = volumes,
                point = point,
                queryMask = mask,
                containedSectionIds = queue.AsParallelWriter()
            }.Schedule(sections.Length, batchSize, dependency);

            var hashHandle = new CleaverSectionIdQueueToHashSetJob {
                containedSectionIds = queue,
                result = results
            }.Schedule(collectHandle);

            return queue.Dispose(hashHandle);
        }

        public void Clear() {
            DiscardBakedData();
        }

        private CleaverProxyGroup[] DiscoverProxyGroups() {
            return FindObjectsByType<CleaverProxyGroup>(FindObjectsInactive.Exclude)
                .Where(FilterValidId)
                .ToArray();

            bool FilterValidId(CleaverProxyGroup group) {
                if (group.Id != 0) return true;
                Debug.LogWarning($"Skipping group with default ID: {group.name}", group);
                return false;
            }
        }

        private CleaverSection[] DiscoverSections() {
            return FindObjectsByType<CleaverSection>(FindObjectsInactive.Exclude)
                .Where(FilterValidId)
                .ToArray();

            bool FilterValidId(CleaverSection section) {
                if (section.Id != 0) return true;
                Debug.LogWarning($"Skipping section with default ID: {section.name}", section);
                return false;
            }
        }

        public CleaverPortal[] DiscoverPortals() {
            return FindObjectsByType<CleaverPortal>(FindObjectsInactive.Exclude)
                .Where(FilterValidId)
                .ToArray();

            bool FilterValidId(CleaverPortal portal) {
                return true; // Portals can have default IDs since they are tracked by their from/to sections
            }
        }

        [Button]
        public void Rebuild() {
            AllocateIfNeeded();
            DiscardBakedData();

            var stopwatch = Stopwatch.StartNew();
            try {
                var groupObjects = DiscoverProxyGroups();
                var proxyCount = groupObjects.Sum(region => region.GetProxyCount());
                var proxyPointCount = groupObjects.Sum(region => region.GetProxySamplePointCount());
                proxyGroups = new NativeArray<CleaverProxyGroupData>(groupObjects.Length, Allocator.Persistent);
                proxies = new NativeArray<CleaverProxyData>(proxyCount, Allocator.Persistent);
                samplePoints = new NativeArray<float3>(proxyPointCount, Allocator.Persistent);

                var pointIndex = 0;
                var proxyIndex = 0;
                for (var i = 0; i < groupObjects.Length; i++) {
                    var group = groupObjects[i];

                    var data = new CleaverProxyGroupData {
                        id = group.Id,
                        bounds = group.ComputeBounds(),
                        mask = (byte)group.mask,
                        proxyIndex = proxyIndex,
                        proxyCount = (byte)group.GetProxyCount()
                    };
                    proxyGroups[i] = data;
                    groupLookup.Add(group.Id, i);
                    managedGroupLookup.Add(group.Id, group);

                    foreach (var proxy in group.GetProxies()) {
                        var proxyData = proxy.InitializeProxyData();
                        proxyData.id = proxy.Id;
                        proxyData.pointIndex = pointIndex;
                        proxies[proxyIndex] = proxyData;
                        proxyLookup.Add(proxy.Id, proxyIndex);
                        managedProxyLookup.Add(proxy.Id, proxy);
                        proxy.SamplePoints(samplePoints, pointIndex);
                        pointIndex += proxy.GetPointCount();
                        proxyIndex++;
                    }
                }

                // Second pass for parent indices
                for (var i = 0; i < groupObjects.Length; i++) {
                    var region = groupObjects[i];
                    var data = proxyGroups[i];
                    data.parentIndex = region.parent ? groupLookup[region.parent.Id] : -1;
                    proxyGroups[i] = data;
                }

                var sectionObjects = DiscoverSections();
                var volumeCount = sectionObjects.Sum(section => section.volumes.Length);
                sections = new NativeArray<CleaverSectionData>(sectionObjects.Length, Allocator.Persistent);
                volumes = new NativeArray<CleaverVolumeData>(volumeCount, Allocator.Persistent);
                var volumeIndex = 0;
                for (var i = 0; i < sectionObjects.Length; i++) {
                    var section = sectionObjects[i];
                    var sectionData = new CleaverSectionData {
                        id = section.Id,
                        mask = (byte)section.mask,
                        closed = section.closed,
                        bounds = section.ComputeBounds(),
                        volumeCount = (byte)section.volumes.Length,
                        volumeIndex = volumeIndex
                    };
                    if (section.proxyGroup) sectionData.regionIndex = groupLookup[section.proxyGroup.Id];
                    section.SampleVolumes(volumes, volumeIndex);
                    section.portals.Clear();
                    sections[i] = sectionData;
                    sectionLookup.Add(section.Id, i);
                    managedSectionLookup.Add(section.Id, section);
                    volumeIndex += section.volumes.Length;
                }

                var portalObjects = DiscoverPortals();
                foreach (var portal in portalObjects) {
                    portal.from?.portals.Add(portal);
                    portal.to?.portals.Add(portal);
                }

                var portalCount = sectionObjects.Sum(section => section.portals.Count);
                portals = new NativeArray<CleaverPortalData>(portalCount, Allocator.Persistent);
                var portalIndex = 0;
                for (var i = 0; i < sectionObjects.Length; i++) {
                    var section = sectionObjects[i];

                    // Second pass update for portal indices
                    var sectionData = sections[i];
                    sectionData.portalIndex = portalIndex;
                    sectionData.portalCount = (byte)section.portals.Count;
                    sections[i] = sectionData;

                    foreach (var portal in section.portals) {
                        var data = new CleaverPortalData {
                            fromIndex = i,
                            toIndex = sectionLookup.TryGetValue(portal.to?.Id ?? 0, out var toIndex) ? toIndex : -1,
                            regionIndex = portal.proxyGroup ? groupLookup[portal.proxyGroup.Id] : -1,
                            open = portal.open
                        };
                        portals[portalIndex] = data;

                        // Gracefully handle self-referencing portals
                        if (portal.from?.Id == section.Id) {
                            portalLookup.Add(portal.IdTo, portalIndex);
                            managedPortalLookup.Add(portal.IdTo, portal);
                        } else if (portal.to?.Id == section.Id) {
                            portalLookup.Add(portal.IdFrom, portalIndex);
                            managedPortalLookup.Add(portal.IdFrom, portal);
                        }

                        portalIndex++;
                    }
                }
            } catch (Exception e) {
                Debug.LogError($"Failed to bake Cleaver environment: {e}");
                DiscardBakedData();
            }

            IncrementVersion();

            try {
                if (!surface.navMeshData) {
                    Debug.Log("No NavMeshData found on surface, creating first version synchronously.", surface);
                    surface.BuildNavMesh();
                } else if (updateNavmeshAsync) {
                    Debug.Log("Updating NavMeshData asynchronously.", surface);
                    surface.UpdateNavMesh(surface.navMeshData);
                } else {
                    Debug.Log("Updating NavMeshData synchronously.", surface);
                    surface.BuildNavMesh();
                }
            } catch (Exception e) { Debug.LogException(e, this); }

            new CleaverEnvironmentRebuiltEvt {
                environment = this
            }.RaiseSafe();

            stopwatch.Stop();
            Debug.Log($"Baked Cleaver environment in {stopwatch.Elapsed.TotalMilliseconds}ms");
        }

        private void DiscardBakedData() {
            managedGroupLookup.Clear();
            managedPortalLookup.Clear();
            managedSectionLookup.Clear();
            managedProxyLookup.Clear();

            proxyGroups.Dispose();
            sections.Dispose();
            volumes.Dispose();
            portals.Dispose();
            proxies.Dispose();
            samplePoints.Dispose();

            if (proxyLookup.IsCreated) proxyLookup.Clear();
            if (portalLookup.IsCreated) portalLookup.Clear();
            if (sectionLookup.IsCreated) sectionLookup.Clear();
            if (groupLookup.IsCreated) groupLookup.Clear();
        }

        private void AllocateIfNeeded() {
            if (!Application.isPlaying) return;

            if (!proxyLookup.IsCreated) proxyLookup = new NativeHashMap<ulong, int>(1, Allocator.Persistent);

            if (!portalLookup.IsCreated) portalLookup = new NativeHashMap<ulong, int>(1, Allocator.Persistent);

            if (!sectionLookup.IsCreated) sectionLookup = new NativeHashMap<ulong, int>(1, Allocator.Persistent);

            if (!groupLookup.IsCreated) groupLookup = new NativeHashMap<ulong, int>(1, Allocator.Persistent);
        }

        private void IncrementVersion() {
            unchecked {
                Version++;
                if (Version == 0) Version++;
            }
        }

        public LayerMask GetMask(ByteMask mask) {
            var mapping = 0;
            for (var index = 0; index < maskMappings.Length; index++) {
                var current = maskMappings[index];
                if ((current.mask & mask) == mask) mapping |= current.included.value;
            }

            return mapping;
        }

    }

    [Serializable]
    public struct MaskMapping {

        [HideLabel]
        public ByteMask mask;
        public LayerMask included;

    }

    public struct CleaverBatchedViewerRefreshEvt : Evt<CleaverBatchedViewerRefreshEvt> {

        public CleaverEnvironment environment;
        public NativeList<JobHandle> batch;

    }

    public struct CleaverBatchedViewerRaycastEvt : Evt<CleaverBatchedViewerRaycastEvt> {

        public CleaverEnvironment environment;
        public NativeList<JobHandle> batch;

    }

    public struct CleaverEnvironmentRebuiltEvt : Evt<CleaverEnvironmentRebuiltEvt> {

        public CleaverEnvironment environment;

        public bool Tracks(CleaverSection section) {
            if (!section || section.Id == 0) return false;
            return environment.sectionLookup.ContainsKey(section.Id);
        }

        public bool Tracks(CleaverProxyGroup group) {
            if (!group || group.Id == 0) return false;
            return environment.groupLookup.ContainsKey(group.Id);
        }
    }

    public struct CleaverCheckForUpdateEvt : Evt<CleaverCheckForUpdateEvt> {

        public CleaverEnvironment environment;
        public bool needsUpdate;
        public bool needsRebuild;

        public void UpdatePortal(CleaverPortal portal) {
            var changed = false;
            if (environment.portalLookup.TryGetValue(portal.IdFrom, out var fromIndex)) {
                var data = environment.portals[fromIndex];
                data.open = portal.open;
                environment.portals[fromIndex] = data;
                changed = true;
            }

            if (environment.portalLookup.TryGetValue(portal.IdTo, out var toIndex)) {
                var data = environment.portals[toIndex];
                data.open = portal.open;
                environment.portals[toIndex] = data;
                changed = true;
            }

            if (changed) needsUpdate = true;
        }

        public void UpdateProxy(CleaverProxy proxy) {
            if (!environment.proxyLookup.TryGetValue(proxy.Id, out var index)) {
                needsRebuild = true;
                return;
            }

            var updatedData = proxy.InitializeProxyData();

            var data = environment.proxies[index];
            data.bounds = updatedData.bounds;
            data.query = updatedData.query;
            data.radius = updatedData.radius;
            environment.proxies[index] = data;

            if (updatedData.pointCount == data.pointCount)
                proxy.SamplePoints(environment.samplePoints, data.pointIndex);
            else
                needsRebuild = true;

            needsUpdate = true;
        }

        public void UpdateProxyGroup(CleaverProxyGroup group) {
            if (!environment.groupLookup.TryGetValue(group.Id, out var index)) {
                needsRebuild = true;
                return;
            }

            var data = environment.proxyGroups[index];
            data.bounds = group.ComputeBounds();
            data.mask = (byte)group.mask;
            environment.proxyGroups[index] = data;

            needsUpdate = true;
        }

        public void UpdateSection(CleaverSection section) {
            if (!environment.sectionLookup.TryGetValue(section.Id, out var index)) {
                needsRebuild = true;
                return;
            }

            var data = environment.sections[index];
            data.bounds = section.ComputeBounds();
            data.mask = (byte)section.mask;
            data.closed = section.closed;
            environment.sections[index] = data;

            if (section.volumes.Length == data.volumeCount)
                section.SampleVolumes(environment.volumes, data.volumeIndex);
            else
                needsRebuild = true;

            needsUpdate = true;
        }

    }

    public struct CleaverEnvironmentStats {

        public int proxyGroupCount;
        public int proxyCount;
        public int portalCount;
        public int volumeCount;
        public int sectionCount;
        public int samplePointCount;
        public uint version;

    }

    public class CleaverNotTrackedException : Exception {

        public CleaverNotTrackedException(string message) : base(message) { }

        public static CleaverNotTrackedException ForProxy(ulong proxyId) {
            return new CleaverNotTrackedException(
                $"Cleaver proxy with ID {proxyId} is not tracked in the environment. " +
                "The CleaverEnvironment must be rebuilt to track this proxy."
            );
        }

        public static CleaverNotTrackedException ForProxyGroup(ulong groupId) {
            return new CleaverNotTrackedException(
                $"Cleaver proxy group with ID {groupId} is not tracked in the environment. " +
                "The CleaverEnvironment must be rebuilt to track this proxy group."
            );
        }

        public static CleaverNotTrackedException ForPortal(ulong portalId) {
            return new CleaverNotTrackedException(
                $"Cleaver portal with ID {portalId} is not tracked in the environment. " +
                "The CleaverEnvironment must be rebuilt to track this portal."
            );
        }

        public static CleaverNotTrackedException ForSection(ulong sectionId) {
            return new CleaverNotTrackedException(
                $"Cleaver section with ID {sectionId} is not tracked in the environment. " +
                "The CleaverEnvironment must be rebuilt to track this section."
            );
        }

    }
}