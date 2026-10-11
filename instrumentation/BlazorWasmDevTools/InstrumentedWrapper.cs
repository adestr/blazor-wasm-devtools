using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorWasmDevTools;

/// <summary>
/// Wrapper component that instruments the lifecycle events of the inner component and notifies DevTools.
/// </summary>
public class InstrumentedWrapper : ComponentBase
{
    private ComponentBase InnerComponent { get; }

    private readonly IEventSink _sink;

    private readonly Guid _wrapperId = Guid.NewGuid();
    private readonly int _componentId;
    private readonly string _componentType;
    private readonly int? _parentComponentId;

    /// <param name="innerComponent"></param>
    public InstrumentedWrapper(ComponentBase innerComponent, IEventSink sink)
    {
        InnerComponent = innerComponent;
        _sink = sink;

        _componentId = InnerComponent.GetHashCode();
        _componentType = InnerComponent.GetType().Name;
        _parentComponentId = null;
    }

    private ComponentLifecycleEvent CreateEvent(ComponentLifecyclePhase phase)
        => EventFactory.Lifecycle(_componentId, _componentType, _parentComponentId, phase);

    protected override void OnInitialized()
    {
        _sink.Publish(CreateEvent(ComponentLifecyclePhase.Initialized));

        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        _sink.Publish(CreateEvent(ComponentLifecyclePhase.Initialized));

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _sink.Publish(CreateEvent(ComponentLifecyclePhase.ParametersSet));

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _sink.Publish(CreateEvent(ComponentLifecyclePhase.AfterRender));

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, InnerComponent);

        base.BuildRenderTree(builder);
    }

    private Dictionary<string, object> CaptureProperties()
    {
        var properties = new Dictionary<string, object>();
        var type = InnerComponent.GetType();
        foreach (var prop in type.GetProperties())
        {
            if (prop.CanRead)
            {
                var value = prop.GetValue(InnerComponent);
                properties[prop.Name] = value ?? "<null>";
            }
        }
        return properties;
    }
}
