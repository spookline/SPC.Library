using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.Loading {
    public class LoadingSystem : Singleton<LoadingSystem> {
        public Dictionary<string, LoadingGroup> Groups { get; } = new();

        public void Clear() {
            lock (this) {
                foreach (var (_,group) in Groups) group.Handles.Clear();
            }
        }
        
        public async UniTask<LoadingState> LoadingGroupTask(string groupName, float timeout, bool clear = true) {
            var group = GetGroup(groupName);
            group.Timeout = Time.time + timeout;
            await UniTask.WaitUntil(() => group.LoadingState != 0);
            var lastState = group.LoadingState;
            if (clear) ClearGroup(groupName);
            return lastState;
        }

        public LoadingGroup GetGroup(string groupName) {
            lock (this) {
                if (Groups.TryGetValue(groupName, out var group)) return group;

                group = new LoadingGroup();
                Groups[groupName] = group;
                return group;
            }
        }

        public void AddHandle(string groupName, ILoadingHandle handle) {
            lock (this) {
                if (Groups.TryGetValue(groupName, out var group)) {
                    group.Handles.Add(handle);
                } else {
                    group = new LoadingGroup();
                    group.Handles.Add(handle);
                    Groups[groupName] = group;
                }
            }
        }

        public void RemoveHandle(string groupName, ILoadingHandle handle) {
            lock (this) {
                if (Groups.TryGetValue(groupName, out var group)) group.Handles.Remove(handle);
            }
        }

        public void ClearGroup(string groupName) {
            lock (this) {
                if (Groups.TryGetValue(groupName, out var group)) group.Handles.Clear();
            }
        }
    }
}