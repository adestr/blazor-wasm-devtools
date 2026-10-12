using BlazorWasmDevTools.Events;
using Microsoft.AspNetCore.Components;

namespace BlazorWasmDevTools.Components;

public partial class InstrumentationProxy(IComponent inner, IEventSink eventSink) : ComponentBase
{
    public IComponent Inner { get; } = inner;

    private int _componentId;
    private string _componentName = default!;
    private Type _componentType = default!;

    public IEventSink EventSink { get; } = eventSink;

    protected override void OnInitialized()
    {
        _componentId = Inner.GetHashCode();
        _componentType = Inner.GetType();
        _componentName = _componentType.Name;

        var e = EventFactory.Lifecycle(_componentId, _componentName, Models.ComponentLifecyclePhase.Initialized);
        EventSink.Publish(e);
    }

    protected override void OnParametersSet()
    {
        // We injected this proxy in place of the inner component, so (the theory is that) Blazor will set the inner component's properties here, and we will relay them to the correct component.
        var e = EventFactory.Lifecycle(_componentId, _componentName, Models.ComponentLifecyclePhase.ParametersSet);
        EventSink.Publish(e);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        var e = EventFactory.Lifecycle(_componentId, _componentName, Models.ComponentLifecyclePhase.AfterRender, firstRender);
        EventSink.Publish(e);
    }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        return Inner.SetParametersAsync(parameters);
    }
}