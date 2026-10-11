using System.Diagnostics.CodeAnalysis;
using BlazorWasmDevTools.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools;

/// <summary>
/// Custom activator which wraps components with an instrumented wrapper to notify DevTools of lifecycle events.
/// </summary>
/// <remarks>
/// Reverts to a simple passthrough for any non-DEBUG build, so instrumentation is only applied in debug builds.
/// </remarks>
public class InstrumentedComponentActivator(IServiceProvider serviceProvider, IEventSink eventSink, DevToolsBridge devToolsBridge, ILogger<InstrumentedComponentActivator> logger) : IComponentActivator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IEventSink _eventSink = eventSink;
    private readonly DevToolsBridge _devToolsBridge = devToolsBridge;
    private readonly ILogger<InstrumentedComponentActivator> _logger = logger;
    private bool _bridgeInitRequested;
    private bool _bridgeInitialized;
    private object _lock = new();

    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        // TODO: Step down the logging level once the instrumentation is stable.
        _logger.LogInformation("Creating instance of component type {ComponentType}", componentType);

        var instance = ActivatorUtilities.CreateInstance(_serviceProvider, componentType) as IComponent
            ?? throw new InvalidOperationException($"Failed to create an instance of component type {componentType.FullName}");

        _ = EnsureDevToolsInitializedAsync();

        if (instance is ComponentBase cb)
        {
            _logger.LogInformation("Wrapping component instance of type {ComponentType} with InstrumentedWrapper", componentType);
            var @event = EventFactory.Generic("Created", []);
            _eventSink.Publish(@event);

            // TODO: We'll need to wrap the component so that it has the same interface
            return new InstrumentedWrapper(cb, _eventSink);
        }

        return instance;
    }

    private async Task EnsureDevToolsInitializedAsync()
    {
        if (_devToolsBridge is null)
        {
            return;
        }
        if (_bridgeInitialized || _bridgeInitRequested)
        {
            return;
        }

        bool wasRequested;
        lock (_lock)
        {
            wasRequested = _bridgeInitRequested;
            _bridgeInitRequested = true;
            if (_bridgeInitialized)
            {
                return;
            }
        }
        if (!wasRequested)
        {
            await _devToolsBridge.InitializeAsync();
            _bridgeInitialized = true;
        }
    }
}
