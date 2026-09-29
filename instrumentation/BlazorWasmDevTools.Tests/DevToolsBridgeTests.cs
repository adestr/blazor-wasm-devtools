using System.Text.Json;
using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Models;
using BlazorWasmDevTools.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using NSubstitute;

namespace BlazorWasmDevTools.Tests;

public sealed class DevToolsBridgeTests
{
    [Fact]
    public async Task InitializeAsync_ImportsModuleAndCallsInitialize()
    {
        var hub = TestEventHub.Create();
        var jsRuntime = Substitute.For<IJSRuntime>();
        var module = Substitute.For<IJSObjectReference>();

        jsRuntime
            .InvokeAsync<IJSObjectReference>(
                "import",
                Arg.Any<object[]>())
            .Returns(new ValueTask<IJSObjectReference>(module));

        var bridge = CreateBridge(jsRuntime, hub);

        await bridge.InitializeAsync();

        await module.Received(1).InvokeVoidAsync("initialize", Arg.Any<object[]>());
    }

    [Fact]
    public void GetSnapshotJson_ReturnsCamelCasePayload()
    {
        var hub = TestEventHub.Create();
        hub.Publish(
            EventFactory.Lifecycle(3, "App", ComponentLifecyclePhase.Initializing, source: "renderer"));

        var bridge = CreateBridge(Substitute.For<IJSRuntime>(), hub);
        var json = bridge.GetSnapshotJson();

        using var document = JsonDocument.Parse(json);
        var components = document.RootElement.GetProperty("components");
        Assert.Equal(JsonValueKind.Array, components.ValueKind);
        Assert.Equal(3, components[0].GetProperty("componentId").GetInt32());
        Assert.Equal("initializing", components[0].GetProperty("lastPhase").GetString());
    }

    [Fact]
    public async Task OnDevToolsActivated_ReplaysBufferedEventsAndRegistersRenderer()
    {
        var hub = TestEventHub.Create();
        var jsRuntime = Substitute.For<IJSRuntime>();
        var module = Substitute.For<IJSObjectReference>();

        jsRuntime
            .InvokeAsync<IJSObjectReference>("import", Arg.Any<object[]>())
            .Returns(new ValueTask<IJSObjectReference>(module));

        var bridge = CreateBridge(jsRuntime, hub);
        await bridge.InitializeAsync();

        hub.Publish(EventFactory.Lifecycle(1, "A", ComponentLifecyclePhase.Rendering, source: "renderer"));

        bridge.OnDevToolsActivated();

        await module.Received(1).InvokeVoidAsync("publishLifecycle", Arg.Any<object[]>());
        await module.Received(1).InvokeVoidAsync("registerRenderer", Arg.Any<object[]>());
        Assert.True(hub.IsActive);
    }

    private static DevToolsBridge CreateBridge(IJSRuntime jsRuntime, IEventSource hub) =>
        new(jsRuntime, hub, NullLogger<DevToolsBridge>.Instance);
}
