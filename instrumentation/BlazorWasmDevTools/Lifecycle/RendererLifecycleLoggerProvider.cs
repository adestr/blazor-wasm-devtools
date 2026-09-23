using System.Collections.Concurrent;
using BlazorWasmDevTools.Models;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools.Lifecycle;

internal sealed class RendererLifecycleLoggerProvider : ILoggerProvider
{
    private readonly ComponentLifecycleHub _hub;
    private readonly ConcurrentDictionary<string, RendererLifecycleLogger> _loggers = new();

    public RendererLifecycleLoggerProvider(ComponentLifecycleHub hub)
    {
        _hub = hub;
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (!string.Equals(
                categoryName,
                "Microsoft.AspNetCore.Components.RenderTree.Renderer",
                StringComparison.Ordinal))
        {
            return NullLogger.Instance;
        }

        return _loggers.GetOrAdd(categoryName, _ => new RendererLifecycleLogger(_hub));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }
}

internal sealed class RendererLifecycleLogger : ILogger
{
    private readonly ComponentLifecycleHub _hub;

    public RendererLifecycleLogger(ComponentLifecycleHub hub)
    {
        _hub = hub;
    }

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

        var lifecycleEvent = _hub.CreateEvent(
            componentId,
            componentType,
            phase.Value,
            parentId,
            source: "renderer");

        _hub.Publish(lifecycleEvent);
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
