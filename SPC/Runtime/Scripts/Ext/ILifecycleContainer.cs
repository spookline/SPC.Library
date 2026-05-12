using System;

namespace Spookline.SPC.Ext {
    public interface ILifecycleContainer {

        public event Action onStart;
        public event Action onEnable;
        public event Action onDisable;

    }

    public static class LifecycleContainerExtensions {

        public static void SubscribeToStart<T>(this T container, Action action)
            where T : ILifecycleContainer, IDisposableContainer {
            container.onStart += action;
            container.DisposeOnDestroy(() => container.onStart -= action);
        }

        public static void SubscribeToEnable<T>(this T container, Action action)
            where T : ILifecycleContainer, IDisposableContainer {
            container.onEnable += action;
            container.DisposeOnDestroy(() => container.onEnable -= action);
        }

        public static void SubscribeToDisable<T>(this T container, Action action)
            where T : ILifecycleContainer, IDisposableContainer {
            container.onDisable += action;
            container.DisposeOnDestroy(() => container.onDisable -= action);
        }

    }
}