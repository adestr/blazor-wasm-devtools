using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Models;
using BlazorWasmDevTools.Tests.TestSupport;
using Microsoft.AspNetCore.Components;

namespace BlazorWasmDevTools.Tests;

public sealed class InstrumentedWrapperTests
{
    [Fact]
    public void OnInitialized_PublishesInitializedLifecycleEvent()
    {
        var sink = new CapturingEventSink();
        var wrapper = new TestableWrapper(new ProbeComponent(), sink);

        wrapper.CallOnInitialized();

        var evt = Assert.Single(sink.Events.OfType<ComponentLifecycleEvent>());
        Assert.Equal(ComponentLifecyclePhase.Initialized, evt.Phase);
        Assert.Equal(nameof(ProbeComponent), evt.ComponentType);
    }

    [Fact]
    public async Task OnParametersSetAsync_PublishesParametersSet()
    {
        var sink = new CapturingEventSink();
        var wrapper = new TestableWrapper(new ProbeComponent(), sink);

        await wrapper.CallOnParametersSetAsync();

        var evt = Assert.Single(sink.Events.OfType<ComponentLifecycleEvent>());
        Assert.Equal(ComponentLifecyclePhase.ParametersSet, evt.Phase);
    }

    [Fact]
    public async Task OnAfterRenderAsync_PublishesAfterRender()
    {
        var sink = new CapturingEventSink();
        var wrapper = new TestableWrapper(new ProbeComponent(), sink);

        await wrapper.CallOnAfterRenderAsync(firstRender: true);

        var evt = Assert.Single(sink.Events.OfType<ComponentLifecycleEvent>());
        Assert.Equal(ComponentLifecyclePhase.AfterRender, evt.Phase);
    }

    private sealed class ProbeComponent : ComponentBase;

    private sealed class TestableWrapper(ComponentBase innerComponent, IEventSink sink)
        : InstrumentedWrapper(innerComponent, sink)
    {
        public void CallOnInitialized() => OnInitialized();

        public Task CallOnParametersSetAsync() => OnParametersSetAsync();

        public Task CallOnAfterRenderAsync(bool firstRender) => OnAfterRenderAsync(firstRender);
    }
}
