using BlazorWasmDevTools.Lifecycle;
using BlazorWasmDevTools.Models;
using BlazorWasmDevTools.Tests.TestSupport;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools.Tests;

public sealed class RendererLifecycleLoggerTests
{
    [Theory]
    [InlineData("InitializingRootComponent", ComponentLifecyclePhase.Initializing)]
    [InlineData("InitializingChildComponent", ComponentLifecyclePhase.Initializing)]
    [InlineData("RenderingComponent", ComponentLifecyclePhase.Rendering)]
    [InlineData("DisposingComponent", ComponentLifecyclePhase.Disposed)]
    public void Log_MapsRendererEventsToLifecyclePhases(string eventName, ComponentLifecyclePhase expectedPhase)
    {
        var hub = new EventHub();
        var logger = new RendererLifecycleLoggerProvider(hub)
            .CreateLogger("Microsoft.AspNetCore.Components.RenderTree.Renderer");

        logger.Log(
            LogLevel.Debug,
            new EventId(1, eventName),
            StructuredLogState.Create(
                ("ComponentId", 12),
                ("ComponentType", typeof(RendererLifecycleLoggerTests).FullName!),
                ("ParentComponentId", 4)),
            null,
            (_, _) => string.Empty);

        var snapshot = hub.GetSnapshot();
        var lifecycleEvent = Assert.Single(snapshot.RecentEvents);

        Assert.Equal(12, lifecycleEvent.ComponentId);
        Assert.Equal(expectedPhase, lifecycleEvent.Phase);
        Assert.Equal("renderer", lifecycleEvent.Source);
        Assert.Equal(4, lifecycleEvent.ParentComponentId);
    }

    [Fact]
    public void CreateLogger_ReturnsDisabledLoggerForOtherCategories()
    {
        var hub = new EventHub();
        var provider = new RendererLifecycleLoggerProvider(hub);
        var logger = provider.CreateLogger("Other.Category");

        Assert.False(logger.IsEnabled(LogLevel.Debug));
    }

    [Fact]
    public void Log_IgnoresUnknownEventNames()
    {
        var hub = new EventHub();
        var logger = new RendererLifecycleLoggerProvider(hub)
            .CreateLogger("Microsoft.AspNetCore.Components.RenderTree.Renderer");

        logger.Log(
            LogLevel.Debug,
            new EventId(99, "HandlingEvent"),
            StructuredLogState.Create(("ComponentId", 1), ("ComponentType", "x")),
            null,
            (_, _) => string.Empty);

        Assert.Empty(hub.GetSnapshot().RecentEvents);
    }
}
