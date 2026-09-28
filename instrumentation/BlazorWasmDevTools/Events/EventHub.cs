using BlazorWasmDevTools.Models;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools.Events;

public sealed class EventHub(ILogger<EventHub> logger) : IEventSource, IEventSink
{
    private const int MaxRecentEvents = 500;

    private readonly Dictionary<int, ComponentDescriptor> _components = new();
    private readonly object _recentLock = new();
    private readonly List<ComponentLifecycleEvent> _recentEvents = [];
    private readonly ILogger<EventHub> _logger = logger;

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

        _logger.LogInformation("Publishing event of type {EventType}", typeof(TEvent).Name);
        // TODO: Implement publishing logic for generic events.
        _logger.LogError("Publishing of event type {EventType} is not implemented", typeof(TEvent).Name);
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
