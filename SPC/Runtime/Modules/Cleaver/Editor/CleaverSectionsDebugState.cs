namespace Spookline.SPC.Cleaver.Editor {
    internal class CleaverSectionsDebugState {

        private static CleaverSectionsDebugState _instance;

        public int selectedSection = -1;
        public bool showPortals = true;

        public bool showSections = true;
        public static CleaverSectionsDebugState Instance => _instance ??= new CleaverSectionsDebugState();

    }
}