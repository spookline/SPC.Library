namespace Spookline.SPC.Audio {
    public enum AudioJobReferenceState {
        Killed = -3,
        Failed = -2,
        Uninitialized = -1,
        Pending = 0,
        Ready = 1
    }
}
