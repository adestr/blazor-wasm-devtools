using BlazorWasmDevTools.Events;

namespace BlazorWasmDevTools.Tests.TestSupport;

internal sealed class CapturingEventSink : IEventSink
{
    public List<IBlazorEvent> Events { get; } = [];

    public void Publish(ComponentLifecycleEvent lifecycleEvent) => Events.Add(lifecycleEvent);

    public void Publish<TEvent>(TEvent @event)
        where TEvent : IBlazorEvent =>
        Events.Add(@event);
}
