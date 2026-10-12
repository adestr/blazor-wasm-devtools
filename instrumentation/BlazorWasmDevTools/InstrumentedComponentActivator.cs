using System.Diagnostics.CodeAnalysis;
using BlazorWasmDevTools.Components;
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
    private readonly InstrumentationFactory _instrumentationFactory = new InstrumentationFactory(
        eventSink,
        serviceProvider.GetRequiredService<ILogger<InstrumentationFactory>>());
    private bool _bridgeInitRequested;
    private bool _bridgeInitialized;
    private object _lock = new();

    public static bool Skip
    {
        get
        {
            var skip = _skip;
            _skip = false;
            return skip;
        }
        set { _skip = value; }
    }
    private static bool _skip;

    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    {
        var instance = ActivatorUtilities.CreateInstance(_serviceProvider, componentType) as IComponent
            ?? throw new InvalidOperationException($"Failed to create an instance of component type {componentType.FullName}");

        _ = EnsureDevToolsInitializedAsync();

        // If the component is already wrapped with an instrumented wrapper, return it as is.
        if (instance.GetType().Name.StartsWith(InstrumentationFactory.Prefix))
        {
            return instance;
        }

        if (instance is ComponentBase)
        {
            var targetType = _instrumentationFactory.Wrap(componentType);
            var wrapped = ActivatorUtilities.CreateInstance(_serviceProvider, targetType) as IComponent
                ?? throw new InvalidOperationException($"Failed to create an instance of component type {targetType.FullName}");
            instance = wrapped;

            var @event = EventFactory.Generic("Created", new Dictionary<string, object?>()
            {
                { "Name", componentType.FullName }
            });
            _eventSink.Publish(@event);
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
