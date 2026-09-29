using BlazorWasmDevTools.Events;

namespace BlazorWasmDevTools.Tests.TestSupport;

internal static class TestEventHub
{
    public static EventHub Create() => new();
}
