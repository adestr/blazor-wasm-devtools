using System.Diagnostics.CodeAnalysis;
using BlazorWasmDevTools.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools;

/// <summary>
/// Custom activator which wraps components with an instrumented wrapper to notify DevTools of lifecycle events.
/// </summary>
public class InstrumentedComponentActivator(IServiceProvider serviceProvider,
                                            IEventSink eventSink,
                                            DevToolsBridge devToolsBridge,
                                            InstrumentationFactory instrumentationFactory,
                                            ILogger<InstrumentedComponentActivator> logger) : IComponentActivator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private readonly IEventSink _eventSink = eventSink;

    private readonly DevToolsBridge _devToolsBridge = devToolsBridge;

    private readonly ILogger<InstrumentedComponentActivator> _logger = logger;

    private readonly InstrumentationFactory _instrumentationFactory = instrumentationFactory;

    private readonly Lazy<Task<bool>> _bridgeInitialized = new(async () =>
    {
        await devToolsBridge.InitializeAsync();
        return true;
    });

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
        if (_devToolsBridge is null || _bridgeInitialized.IsValueCreated)
        {
            return;
        }
        await _bridgeInitialized.Value;
    }
}
