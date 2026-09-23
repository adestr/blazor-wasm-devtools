using BlazorWasmDevTools.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorWasmDevTools.Tests.Components;

public sealed class LifecycleProbeComponent : DevToolsComponentBase
{
    [Parameter]
    public int Value { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, Value);
    }
}
