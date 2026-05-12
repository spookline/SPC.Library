using System;
using Cysharp.Threading.Tasks;
using Spookline.SPC.Events;

namespace Spookline.SPC.Ext {
  public readonly struct EventCallbackBuilder<T> where T : Evt<T> {

    private readonly IDisposableContainer _container;
    private readonly EventReactor<T> _reactor;

    public EventCallbackBuilder(IDisposableContainer container) {
      _container = container;
      _reactor = Evt<T>.Reactor;
    }

    public EventCallbackBuilder(IDisposableContainer container, EventReactor<T> reactor) {
      _container = container;
      _reactor = reactor;
    }

    public HandlerRegistration<T> Do(EventHandler<T> action, int priority = 0, string debugName = null) {
#if DEBUG
      if (debugName == null) {
        var clazz = _container.GetType().FullName;
        var name = EventReactorInfo.GetDebugName(action);
        debugName = $"@{clazz} {name}";
      }
#endif

      var registration = _reactor.Subscribe(action, priority, debugName);
      _container.DisposeOnDestroy(registration);
      return registration;
    }

    public HandlerRegistration<T> Do(ConsumerEventHandler<T> action, int priority = 0, string debugName = null) {
#if DEBUG
      if (debugName == null) {
        var clazz = _container.GetType().FullName;
        var name = EventReactorInfo.GetDebugName(action);
        debugName = $"@{clazz} {name}";
      }
#endif

      var registration = _reactor.Subscribe(action, priority, debugName);
      _container.DisposeOnDestroy(registration);
      return registration;
    }


    public HandlerRegistration<T> Stream(
      StreamEventHandler<T> action,
      int priority = 0,
      string debugName = null
    ) {
#if DEBUG
      if (debugName == null) {
        var clazz = _container.GetType().FullName;
        var name = EventReactorInfo.GetDebugName(action);
        debugName = $"@{clazz} {name}";
      }
#endif
      var registration = _reactor.SubscribeStream(action, priority, debugName);
      _container.DisposeOnDestroy(registration);
      return registration;
    }

    public HandlerRegistration<T> DoOnce(EventHandler<T> action, int priority = 0, string debugName = null) {
#if DEBUG
      if (debugName == null) {
        var clazz = _container.GetType().FullName;
        var name = EventReactorInfo.GetDebugName(action);
        debugName = $"@{clazz} {name}";
      }
#endif
      var registration = _reactor.SubscribeOnce(action, priority, debugName);
      _container.DisposeOnDestroy(registration);
      return registration;
    }

    public HandlerRegistration<T> DoOnce(ConsumerEventHandler<T> action, int priority = 0, string debugName = null) {
#if DEBUG
      if (debugName == null) {
        var clazz = _container.GetType().FullName;
        var name = EventReactorInfo.GetDebugName(action);
        debugName = $"@{clazz} {name}";
      }
#endif
      var registration = _reactor.SubscribeOnce(action, priority, debugName);
      _container.DisposeOnDestroy(registration);
      return registration;
    }

  }


  public static class EventCallbackBuilderExtensions {

    public static HandlerRegistration<T> Do<T>(
      this EventCallbackBuilder<T> builder,
      Func<T, UniTask> action,
      int priority = 0,
      string debugName = null
    ) where T : Evt<T> {
      return builder.Do(evt => { action(evt).Forget(); }, priority, debugName);
    }

    public static HandlerRegistration<T> AsyncDo<T>(
      this EventCallbackBuilder<T> builder,
      Func<T, UniTask> action,
      int priority = 0,
      string debugName = null
    ) where T : AsyncChainEvt<T> =>
      ChainDo(builder, action, priority, debugName);

    public static HandlerRegistration<T> AsyncDo<T>(
      this EventCallbackBuilder<T> builder,
      Action<T> action,
      int priority = 0,
      string debugName = null
    ) where T : AsyncChainEvt<T> =>
      ChainDo(builder, action, priority, debugName);

    // Newer clearer versions
    public static HandlerRegistration<T> ChainDo<T>(
      this EventCallbackBuilder<T> builder,
      Func<T, UniTask> action,
      int priority = 0,
      string debugName = null
    ) where T : AsyncChainEvt<T> {
      return builder.Do(
        evt => { evt.Chain += async () => { await action(evt); }; },
        priority,
        debugName
      );
    }

    public static HandlerRegistration<T> ChainDo<T>(
      this EventCallbackBuilder<T> builder,
      Action<T> action,
      int priority = 0,
      string debugName = null
    ) where T : AsyncChainEvt<T> {
      return builder.Do(
        evt => {
          evt.Chain += () => {
            action(evt);
            return UniTask.CompletedTask;
          };
        },
        priority,
        debugName
      );
    }

  }
}