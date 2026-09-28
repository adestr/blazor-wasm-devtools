using System.Collections.Concurrent;
using BlazorWasmDevTools.Events;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools.Lifecycle;

internal sealed class RendererLifecycleLoggerProvider(EventHub hub) : ILoggerProvider
{
    private readonly EventHub _hub = hub;
    private readonly ConcurrentDictionary<string, RendererLifecycleLogger> _loggers = new();

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
