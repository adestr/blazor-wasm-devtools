using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using NSubstitute;

namespace BlazorWasmDevTools.Tests;

public sealed class InstrumentedComponentActivatorTests
{
    // [Fact]
    // public void CreateInstance_WrapsComponentBaseAndPublishesCreatedEvent()
    // {
    //     var sink = new CapturingEventSink();
    //     var services = new ServiceCollection();
    //     services.AddSingleton<IEventSink>(sink);
    //     using var provider = services.BuildServiceProvider();

    //     var activator = new InstrumentedComponentActivator(
    //         provider,
    //         sink,
    //         CreateBridge(),
    //         NullLogger<InstrumentedComponentActivator>.Instance);

    //     var instance = activator.CreateInstance(typeof(ProbeComponent));

    //     Assert.IsType<InstrumentedWrapper>(instance);
    //     var created = Assert.Single(sink.Events.OfType<BlazorEvent>());
    //     Assert.Equal("Created", created.Name);
    // }

    // [Fact]
    // public void CreateInstance_PassesThroughNonComponentBase()
    // {
    //     var sink = new CapturingEventSink();
    //     var services = new ServiceCollection();
    //     using var provider = services.BuildServiceProvider();

    //     var activator = new InstrumentedComponentActivator(
    //         provider,
    //         sink,
    //         CreateBridge(),
    //         NullLogger<InstrumentedComponentActivator>.Instance);

    //     var instance = activator.CreateInstance(typeof(RawComponent));

    //     Assert.IsType<RawComponent>(instance);
    //     Assert.Empty(sink.Events);
    // }

    private static DevToolsBridge CreateBridge()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        var module = Substitute.For<IJSObjectReference>();
        jsRuntime
            .InvokeAsync<IJSObjectReference>("import", Arg.Any<object[]>())
            .Returns(new ValueTask<IJSObjectReference>(module));

        return new DevToolsBridge(jsRuntime, TestEventHub.Create(), NullLogger<DevToolsBridge>.Instance);
    }

    private sealed class ProbeComponent : ComponentBase;

    private sealed class RawComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}
