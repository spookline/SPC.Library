namespace Spookline.SPC.Cleaver.Editor {
    internal class CleaverRegionsDebugState {

        private static CleaverRegionsDebugState _instance;

        public int selectedGroup = -1;
        public int selectedProxy = -1;

        public bool showGroups = true;
        public bool showProxies;
        public bool showSamplePoints;
        public static CleaverRegionsDebugState Instance => _instance ??= new CleaverRegionsDebugState();

    }
}