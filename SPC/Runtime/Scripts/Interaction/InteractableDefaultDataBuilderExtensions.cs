using System;

namespace Spookline.SPC.Interaction {
    public static class InteractableDefaultDataBuilderExtensions {

        public static InteractableBuilder WithText(this InteractableBuilder builder, Func<string> text) {
            return builder.WithData("text", text);
        }

        public static InteractableBuilder WithText(this InteractableBuilder builder, string text) {
            return builder.WithText(() => text);
        }

        public static Func<string> GetText(this Interactable interactable) =>
            interactable.GetDataAsFunc<string>("text");
        
        public static bool HasText(this Interactable interactable) =>
            interactable.HasData("text");

    }
}