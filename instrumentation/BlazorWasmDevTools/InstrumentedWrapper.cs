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
    private IComponent InnerComponent { get; }

    private readonly IEventSink _sink;

    private readonly Guid _wrapperId = Guid.NewGuid();

    private readonly int _componentId;

    private readonly string _componentName;

    private readonly Type _componentType;

    private readonly int? _parentComponentId;

    /// <param name="innerComponent"></param>
    public InstrumentedWrapper(IComponent innerComponent, IEventSink sink)
    {
        InnerComponent = innerComponent;
        _sink = sink;

        _componentId = InnerComponent.GetHashCode();
        _componentType = InnerComponent.GetType();
        _componentName = _componentType.Name;
        _parentComponentId = null;
    }

    private ComponentLifecycleEvent CreateEvent(ComponentLifecyclePhase phase)
        => EventFactory.Lifecycle(_componentId, _componentName, _parentComponentId, phase);

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
        var e = EventFactory.Lifecycle(_componentId, _componentName, _parentComponentId, ComponentLifecyclePhase.AfterRender, firstRender);
        _sink.Publish(e);

        await base.OnAfterRenderAsync(firstRender);
    }

    // We're not actually rendering the component here, we're rendering a duplicate of the component with the same attributes.
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        int seq = 0;
        // This is disgusting
        InstrumentedComponentActivator.Skip = true;
        builder.OpenComponent(seq++, _componentType);

        var properties = _componentType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        foreach (var p in properties)
        {
            var getter = p.GetMethod;
            var value = getter?.Invoke(InnerComponent, null);
            if (value is not null)
            {
                builder.AddAttribute(seq++, p.Name, value);
            }
            else
            {
                seq++;
            }
        }

        builder.CloseComponent();

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
