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
public class InstrumentedComponentActivator(IServiceProvider serviceProvider, IEventSink eventSink, ILogger<InstrumentedComponentActivator> logger) : IComponentActivator
{
    public readonly IServiceProvider _serviceProvider = serviceProvider;
    public readonly IEventSink _eventSink = eventSink;
    public readonly ILogger<InstrumentedComponentActivator> _logger = logger;

    public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
    //public IComponent CreateInstance(Type componentType)
    {
        // TODO: Step down the logging level once the instrumentation is stable.
        _logger.LogInformation("Creating instance of component type {ComponentType}", componentType);

        var instance = ActivatorUtilities.CreateInstance(_serviceProvider, componentType) as IComponent
            ?? throw new InvalidOperationException($"Failed to create an instance of component type {componentType.FullName}");

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
}
