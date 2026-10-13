using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorWasmDevTools.Tests.Components;

public class TestComponent : ComponentBase
{
    public string TestString { get; set; } = "";

    public int TestInt { get; set; }

    public int BRTInvocationCount { get; set; } = 0;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        BRTInvocationCount++;
        base.BuildRenderTree(builder);
    }
}