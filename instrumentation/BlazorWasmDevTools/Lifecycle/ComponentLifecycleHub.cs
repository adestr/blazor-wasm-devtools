using BlazorWasmDevTools.Models;

namespace BlazorWasmDevTools.Lifecycle;

public sealed class ComponentLifecycleHub : IComponentLifecycleSink
{
    private const int MaxRecentEvents = 500;

    private readonly Dictionary<int, ComponentDescriptor> _components = new();
    private readonly object _recentLock = new();
    private readonly List<ComponentLifecycleEvent> _recentEvents = [];

    private long _sequence;
    private volatile bool _isActive;

    public event Action<ComponentLifecycleEvent>? LifecyclePublished;

    public bool IsActive => _isActive;

    public void SetActive(bool active)
    {
        _isActive = active;
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

    public ComponentLifecycleEvent CreateEvent(
        int componentId,
        string componentType,
        ComponentLifecyclePhase phase,
        int? parentComponentId = null,
        bool firstRender = false,
        string source = "instrumentation")
    {
        return new ComponentLifecycleEvent(
            Interlocked.Increment(ref _sequence),
            componentId,
            componentType,
            parentComponentId,
            phase,
            firstRender,
            source,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
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
