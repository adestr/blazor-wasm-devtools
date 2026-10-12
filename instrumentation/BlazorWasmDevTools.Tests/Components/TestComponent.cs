using Microsoft.AspNetCore.Components;

namespace BlazorWasmDevTools.Tests.Components;

public class TestComponent : ComponentBase
{
    public string TestString { get; set; } = "";

    public int TestInt { get; set; }
}