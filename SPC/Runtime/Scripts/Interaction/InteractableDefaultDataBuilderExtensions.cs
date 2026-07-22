using System;
using UnityEngine;

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

        public static InteractableBuilder WithOutline(this InteractableBuilder builder, Func<MeshRenderer[]> renderers) {
            return builder.WithData("outline", renderers);
        }
        
        public static InteractableBuilder WithOutline(this InteractableBuilder builder, MeshRenderer[] renderers) {
            return builder.WithOutline(() => renderers);
        }
        
        public static Func<MeshRenderer[]> GetOutline(this Interactable interactable) =>
            interactable.GetDataAsFunc<MeshRenderer[]>("outline");

        public static bool HasOutline(this Interactable interactable) =>
            interactable.HasData("outline");

    }
}