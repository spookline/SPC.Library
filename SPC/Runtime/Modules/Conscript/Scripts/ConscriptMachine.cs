using System.Collections.Generic;
using HELIX.Widgets.Diagnostics;
using Spookline.SPC.Conscript.UI;
using UnityEngine;

namespace Spookline.SPC.Conscript {
    public class ConscriptMachine : DiagnosticableTreeBase {

        public ConscriptNode Root { get; }
        public List<ConscriptNode> Nodes { get; }

        public uint CurrentTick { get; private set; }
        private bool _hasYielded = false;
        public uint RecordingStart { get; private set; }

        public bool Recording { get; private set; } = false;

        public static uint GetNextTick(uint lastTick) {
            if (lastTick == uint.MaxValue) return 1;
            return lastTick + 1;
        }

        public static uint GetPreviousTick(uint tick) {
            return tick switch {
                0 => 0,
                1 => uint.MaxValue,
                _ => tick - 1
            };
        }

        public bool WasTickedLastFrame(uint lastTick) {
            if (lastTick == 0 || CurrentTick == 0) return false;
            return CurrentTick == GetNextTick(lastTick);
        }

        public bool IsInitial => CurrentTick > 0;

        public ConscriptMachine(ConscriptNode root) {
            Root = root;
            var visited = new List<ConscriptNode>();
            Link(root, visited);
            Nodes = visited;
        }

        private void Link(ConscriptNode node, List<ConscriptNode> visited) {
            if (visited.Contains(node)) return;
            node.PostInit();
            node.Machine = this;
            node.NodeIndex = visited.Count;
            node.LastTick = 0;
            visited.Add(node);

            var interrupters = new List<ConscriptNode>();
            foreach (var child in node.children) {
                child.Parent = node;
                if (interrupters.Count > 0) child.Interrupters = interrupters.ToArray();

                Link(child, visited);
                if (child.IsInterrupter) interrupters.Add(child);
            }
        }

        /// <summary>
        /// Performs a single full tick of the behavior hierarchy tree.
        /// </summary>
        public void Tick() {
            _hasYielded = false;
            CurrentTick = GetNextTick(CurrentTick);

            if (CurrentTick == 1 && Recording) {
                SetRecording(false);
                SetRecording(true);
                Debug.Log("Behavior tree wrap around, clearing recording state");
            }

            Root.RootTick();
            if (_hasYielded) {
                Debug.Log("Behavior tree yielded during tick");
                Tick();
            }
        }

        /// <summary>
        /// Fully resets the behavior hierarchy tree.
        /// </summary>
        public void Reset() {
            _hasYielded = false;
            CurrentTick = 0;
            Root.Reset();
        }

        public void SetRecording(bool record) {
            if (record == Recording) return;
            if (record) {
                RecordingStart = CurrentTick;
            } else {
                RecordingStart = 0;
            }
            Recording = record;
            foreach (var node in Nodes) { node.SetRecording(record); }
        }

        internal void NotifyYield() {
            _hasYielded = true;
        }

        public NodeTemplate BuildWidget() {
            return Root.BuildWidget();
        }

        public override void DebugFillProperties(DiagnosticPropertiesBuilder properties) {
            base.DebugFillProperties(properties);
            properties.Add(new DiagnosticsProperty<uint>("Tick", CurrentTick));
        }

        public override List<DiagnosticsNode> DebugDescribeChildren() {
            return new List<DiagnosticsNode> { Root.ToDiagnosticsNodeSafe() };
        }

    }
}