using BlazorWasmDevTools.Models;

namespace BlazorWasmDevTools.Events;

public interface IEventSink
{
    void Publish(ComponentLifecycleEvent lifecycleEvent);

    void Publish<TEvent>(TEvent @event)
        where TEvent : IBlazorEvent;
}
