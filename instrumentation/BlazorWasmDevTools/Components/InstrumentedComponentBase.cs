using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Lifecycle;
using BlazorWasmDevTools.Models;
using Microsoft.AspNetCore.Components;

namespace BlazorWasmDevTools.Components;

/// <summary>
/// Optional base component that reports Blazor lifecycle phases to DevTools in addition to renderer logging.
/// </summary>
public abstract class InstrumentedComponentBase : ComponentBase
{
    [Inject]
    private EventHub LifecycleHub { get; set; } = default!;

    private int _componentId;
    private bool _hasRendered;

    protected override void OnInitialized()
    {
        _componentId = ComponentInstanceIds.GetOrAssign(this);
        Publish(ComponentLifecyclePhase.Initialized);
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        if (_componentId == 0)
        {
            _componentId = ComponentInstanceIds.GetOrAssign(this);
        }

        Publish(ComponentLifecyclePhase.ParametersSet);
        base.OnParametersSet();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        Publish(ComponentLifecyclePhase.AfterRender, firstRender);
        _hasRendered = true;
        base.OnAfterRender(firstRender);
    }

    private void Publish(ComponentLifecyclePhase phase, bool firstRender = false)
    {
        var lifecycleEvent = EventFactory.Lifecycle(
            _componentId,
            GetType().FullName ?? GetType().Name,
            phase,
            firstRender: firstRender || (!_hasRendered && phase == ComponentLifecyclePhase.AfterRender),
            source: "component-base");

        LifecycleHub.Publish(lifecycleEvent);
    }
}
