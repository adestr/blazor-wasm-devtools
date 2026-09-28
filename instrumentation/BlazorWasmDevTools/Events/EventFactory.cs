using BlazorWasmDevTools.Models;

namespace BlazorWasmDevTools.Events;

public sealed class EventFactory()
{
    private static long _sequence = 0;

    /// <summary>
    /// Creates a new instance of <see cref="ComponentLifecycleEvent"/>.
    /// </summary>
    /// <param name="componentId"></param>
    /// <param name="componentType"></param>
    /// <param name="parentComponentId"></param>
    /// <param name="phase"></param>
    /// <param name="firstRender"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static ComponentLifecycleEvent Lifecycle(
        int componentId,
        string componentType,
        int? parentComponentId,
        ComponentLifecyclePhase phase,
        bool firstRender = false,
        string source = "instrumentation"
    )
    {
        return new(
            Interlocked.Increment(ref _sequence),
            componentId,
            componentType,
            parentComponentId,
            phase,
            firstRender,
            source,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );
    }

    /// <summary>
    /// Creates a new instance of <see cref="ComponentLifecycleEvent"/>.
    /// </summary>
    /// <param name="componentId"></param>
    /// <param name="componentType"></param>
    /// <param name="phase"></param>
    /// <param name="firstRender"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static ComponentLifecycleEvent Lifecycle(
        int componentId,
        string componentType,
        ComponentLifecyclePhase phase,
        bool firstRender = false,
        string source = "instrumentation"
    )
    {
        return new(
            Interlocked.Increment(ref _sequence),
            componentId,
            componentType,
            null,
            phase,
            firstRender,
            source,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );
    }
    public static BlazorEvent Generic(
        string name,
        Dictionary<string, object?> properties,
        string source = "instrumentation"
    )
    {
        return new(
            Interlocked.Increment(ref _sequence),
            name,
            properties,
            source,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );
    }
}

