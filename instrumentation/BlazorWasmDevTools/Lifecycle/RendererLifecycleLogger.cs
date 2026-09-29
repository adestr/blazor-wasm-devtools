using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Models;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools.Lifecycle;

internal sealed class RendererLifecycleLogger(IEventSink eventSink) : ILogger
{
    private readonly IEventSink _eventSink = eventSink;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel < LogLevel.Debug)
        {
            return;
        }

        if (state is not IReadOnlyList<KeyValuePair<string, object?>> values)
        {
            return;
        }

        var phase = MapPhase(eventId.Name);
        if (phase is null)
        {
            return;
        }

        if (!TryGetInt(values, "ComponentId", out var componentId))
        {
            return;
        }

        var componentType = TryGetString(values, "ComponentType") ?? "unknown";
        int? parentId = TryGetInt(values, "ParentComponentId", out var parsedParent)
            ? parsedParent
            : null;

        var lifecycleEvent = EventFactory.Lifecycle(
            componentId,
            componentType,
            parentId,
            phase.Value,
            source: "renderer");

        _eventSink.Publish(lifecycleEvent);
    }

    private static ComponentLifecyclePhase? MapPhase(string? eventName) =>
        eventName switch
        {
            "InitializingRootComponent" or "InitializingChildComponent" => ComponentLifecyclePhase.Initializing,
            "RenderingComponent" => ComponentLifecyclePhase.Rendering,
            "DisposingComponent" => ComponentLifecyclePhase.Disposed,
            _ => null,
        };

    private static bool TryGetInt(
        IReadOnlyList<KeyValuePair<string, object?>> values,
        string name,
        out int result)
    {
        result = default;
        foreach (var pair in values)
        {
            if (!string.Equals(pair.Key, name, StringComparison.Ordinal))
            {
                continue;
            }

            return pair.Value switch
            {
                int i => (result = i) == i,
                long l => (result = (int)l) == (int)l,
                string s when int.TryParse(s, out result) => true,
                _ => false,
            };
        }

        return false;
    }

    private static string? TryGetString(IReadOnlyList<KeyValuePair<string, object?>> values, string name)
    {
        foreach (var pair in values)
        {
            if (!string.Equals(pair.Key, name, StringComparison.Ordinal))
            {
                continue;
            }

            return pair.Value switch
            {
                null => null,
                Type type => type.FullName ?? type.Name,
                string s => s,
                _ => pair.Value.ToString(),
            };
        }

        return null;
    }
}
