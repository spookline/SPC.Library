using System.Collections.Generic;
using System.Linq;
using Time = UnityEngine.Time;

namespace Spookline.SPC.Loading {

    public interface ILoadingHandle {
        bool IsLoadingInProgress { get; }
    }

    public class LoadingGroup {
        public float Timeout { get; set; } = float.MaxValue;
        public List<ILoadingHandle> Handles { get; } = new();

        public float Progress {
            get {
                if (Handles.Count == 0) return 1f;
                var loadingCount = Handles.Count(handle => handle.IsLoadingInProgress);
                return (float)loadingCount / Handles.Count;
            }
        }

        public LoadingState LoadingState {
            get {
                if (Time.time > Timeout) return LoadingState.Timeout;

                return Handles.Any(handle => handle.IsLoadingInProgress) ? LoadingState.Loading : LoadingState.Loaded;
            }
        }
    }

    public enum LoadingState {
        Timeout = -1,
        Loading = 0,
        Loaded = 1
    }
}