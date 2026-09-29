using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Models;
using BlazorWasmDevTools.Tests.TestSupport;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWasmDevTools.Tests.Components;

public sealed class DevToolsComponentBaseTests : BunitContext
{
    [Fact]
    public void FirstRender_PublishesInitializedParametersSetAndAfterRender()
    {
        var hub = RegisterHub();

        Render<LifecycleProbeComponent>(parameters => parameters.Add(p => p.Value, 1));

        var phases = ComponentBasePhases(hub);
        Assert.Equal(
            [
                ComponentLifecyclePhase.Initialized,
                ComponentLifecyclePhase.ParametersSet,
                ComponentLifecyclePhase.AfterRender,
            ],
            phases);
    }

    [Fact]
    public void FirstRender_UsesStableComponentIdAndComponentBaseSource()
    {
        var hub = RegisterHub();

        Render<LifecycleProbeComponent>();

        var events = ComponentBaseEvents(hub);
        Assert.Equal(3, events.Count);
        Assert.All(events, evt => Assert.Equal("component-base", evt.Source));
        Assert.All(events, evt => Assert.Equal(events[0].ComponentId, evt.ComponentId));
        Assert.Contains(
            events,
            evt => evt.ComponentType == typeof(LifecycleProbeComponent).FullName);
    }

    [Fact]
    public void FirstRender_AfterRenderEventIsMarkedAsFirstRender()
    {
        var hub = RegisterHub();

        Render<LifecycleProbeComponent>();

        var afterRender = Assert.Single(
            ComponentBaseEvents(hub),
            evt => evt.Phase == ComponentLifecyclePhase.AfterRender);

        Assert.True(afterRender.FirstRender);
    }

    [Fact]
    public void ReRenderWithNewParameters_PublishesParametersSetAndAfterRenderAgain()
    {
        var hub = RegisterHub();

        var cut = Render<LifecycleProbeComponent>(parameters => parameters.Add(p => p.Value, 1));
        cut.Render(parameters => parameters.Add(p => p.Value, 2));

        var phases = ComponentBasePhases(hub);
        Assert.Equal(
            [
                ComponentLifecyclePhase.Initialized,
                ComponentLifecyclePhase.ParametersSet,
                ComponentLifecyclePhase.AfterRender,
                ComponentLifecyclePhase.ParametersSet,
                ComponentLifecyclePhase.AfterRender,
            ],
            phases);
    }

    [Fact]
    public void ReRender_SubsequentAfterRenderIsNotFirstRender()
    {
        var hub = RegisterHub();

        var cut = Render<LifecycleProbeComponent>(parameters => parameters.Add(p => p.Value, 1));
        cut.Render(parameters => parameters.Add(p => p.Value, 2));

        var afterRenderEvents = ComponentBaseEvents(hub)
            .Where(evt => evt.Phase == ComponentLifecyclePhase.AfterRender)
            .ToList();

        Assert.Equal(2, afterRenderEvents.Count);
        Assert.True(afterRenderEvents[0].FirstRender);
        Assert.False(afterRenderEvents[1].FirstRender);
    }

    private EventHub RegisterHub()
    {
        var hub = TestEventHub.Create();
        Services.AddSingleton(hub);
        return hub;
    }

    private static IEnumerable<ComponentLifecyclePhase> ComponentBasePhases(EventHub hub) =>
        ComponentBaseEvents(hub).Select(evt => evt.Phase);

    private static List<ComponentLifecycleEvent> ComponentBaseEvents(EventHub hub) =>
        hub.GetSnapshot()
            .RecentEvents
            .Where(evt => evt.Source == "component-base")
            .ToList();
}
