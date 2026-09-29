using BlazorWasmDevTools.Models;

namespace BlazorWasmDevTools.Events;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// This class can not accept an <see cref="ILogger{EventHub}" /> argument, as it is consumed by <see cref="RendererLifecycleLoggerProvider" />.
public sealed class EventHub() : IEventSource, IEventSink
{
    private const int MaxRecentEvents = 500;

    private readonly Dictionary<int, ComponentDescriptor> _components = new();
    private readonly object _recentLock = new();
    private readonly List<ComponentLifecycleEvent> _recentEvents = [];

    // private long _sequence;
    private volatile bool _isActive;

    public event Action<ComponentLifecycleEvent>? LifecyclePublished;

    public bool IsActive => _isActive;

    public void SetActive(bool active)
    {
        _isActive = active;
    }

    public void Publish<TEvent>(TEvent @event)
        where TEvent : IBlazorEvent
    {
        if (@event is ComponentLifecycleEvent lifecycleEvent)
        {
            Publish(lifecycleEvent);
        }

        // TODO: Implement publishing logic for generic events.
    }

    public void Publish(ComponentLifecycleEvent lifecycleEvent)
    {
        var descriptor = new ComponentDescriptor(
            lifecycleEvent.ComponentId,
            lifecycleEvent.ComponentType,
            lifecycleEvent.ParentComponentId,
            lifecycleEvent.Phase,
            lifecycleEvent.TimestampUnixMilliseconds);

        _components[lifecycleEvent.ComponentId] = descriptor;

        lock (_recentLock)
        {
            _recentEvents.Add(lifecycleEvent);
            if (_recentEvents.Count > MaxRecentEvents)
            {
                _recentEvents.RemoveRange(0, _recentEvents.Count - MaxRecentEvents);
            }
        }

        if (_isActive)
        {
            LifecyclePublished?.Invoke(lifecycleEvent);
        }
    }

    public DevToolsComponentSnapshot GetSnapshot()
    {
        lock (_recentLock)
        {
            return new DevToolsComponentSnapshot(
                _components.Values.OrderBy(c => c.ComponentId).ToArray(),
                _recentEvents.ToArray());
        }
    }
}
