namespace Spookline.SPC.Interaction {
    public abstract class IInteractablePreProcessor {

        public virtual bool CanBegin(InteractionContext context) => true;
        
        public virtual void Begin(InteractionContext context) { }
        
        public abstract InteractionProcessResult Process(InteractionContext context);
        
        public virtual void Cancel(InteractionContext context) { }
        
        public virtual void Reset() { }

    }

    public enum InteractionProcessResult {

        Running,
        
        Completed,
        
        Rejected

    }

    
}