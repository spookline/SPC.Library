namespace Spookline.SPC.Interaction {
    public sealed class InteractionContext {

        public InteractionManager Manager { get; private set; }
        
        public Interactable Interactable { get; private set; }
        
        public float DeltaTime { get; private set; }
        
        public float UnscaledDeltaTime { get; private set; }
        
        public bool IsInputHeld { get; private set; }

        internal InteractionContext(InteractionManager manager, Interactable interactable) {
            Manager = manager;
            Interactable = interactable;
        }
        
        internal void SetFrameData(float deltaTime, float unscaledDeltaTime, bool isInputHeld) {
            DeltaTime = deltaTime;
            UnscaledDeltaTime = unscaledDeltaTime;
            IsInputHeld = isInputHeld;
        }

    }
}