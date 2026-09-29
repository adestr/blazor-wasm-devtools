using AutoFixture;
using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Models;
using BlazorWasmDevTools.Tests.TestSupport;

namespace BlazorWasmDevTools.Tests;

public sealed class EventHubTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Publish_WhenInactive_DoesNotRaiseLifecyclePublished()
    {
        var hub = TestEventHub.Create();
        var raised = 0;
        hub.LifecyclePublished += _ => raised++;

        hub.Publish(CreateEvent());

        Assert.Equal(0, raised);
        Assert.Single(hub.GetSnapshot().RecentEvents);
    }

    [Fact]
    public void Publish_WhenActive_RaisesLifecyclePublished()
    {
        var hub = TestEventHub.Create();
        hub.SetActive(true);
        ComponentLifecycleEvent? published = null;
        hub.LifecyclePublished += evt => published = evt;

        var lifecycleEvent = CreateEvent();
        hub.Publish(lifecycleEvent);

        Assert.Equal(lifecycleEvent, published);
    }

    [Fact]
    public void GetSnapshot_TracksComponentsAndRecentEvents()
    {
        var hub = TestEventHub.Create();
        var lifecycleEvent = EventFactory.Lifecycle(
            7,
            "Sample.App",
            parentComponentId: 3,
            phase: ComponentLifecyclePhase.Initializing,
            source: "renderer");

        hub.Publish(lifecycleEvent);

        var snapshot = hub.GetSnapshot();

        Assert.Single(snapshot.Components);
        Assert.Equal(7, snapshot.Components[0].ComponentId);
        Assert.Equal(ComponentLifecyclePhase.Initializing, snapshot.Components[0].LastPhase);
        Assert.Equal(3, snapshot.Components[0].ParentComponentId);
        Assert.Single(snapshot.RecentEvents);
    }

    [Fact]
    public void Publish_TrimsRecentEventsTo500()
    {
        var hub = TestEventHub.Create();

        for (var i = 0; i < 501; i++)
        {
            hub.Publish(EventFactory.Lifecycle(i, "T", ComponentLifecyclePhase.Rendering));
        }

        Assert.Equal(500, hub.GetSnapshot().RecentEvents.Count);
        Assert.Equal(1, hub.GetSnapshot().RecentEvents[0].ComponentId);
    }

    [Fact]
    public void Publish_GenericLifecycleEvent_StoresInSnapshot()
    {
        var hub = TestEventHub.Create();
        IBlazorEvent lifecycleEvent = EventFactory.Lifecycle(
            9,
            "Generic.Path",
            ComponentLifecyclePhase.Rendering);

        hub.Publish(lifecycleEvent);

        Assert.Equal(9, Assert.Single(hub.GetSnapshot().RecentEvents).ComponentId);
    }

    private ComponentLifecycleEvent CreateEvent() =>
        _fixture.Build<ComponentLifecycleEvent>()
            .With(e => e.Phase, ComponentLifecyclePhase.AfterRender)
            .Create();
}
