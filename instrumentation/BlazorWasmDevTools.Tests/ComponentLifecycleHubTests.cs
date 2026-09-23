using AutoFixture;
using BlazorWasmDevTools.Lifecycle;
using BlazorWasmDevTools.Models;

namespace BlazorWasmDevTools.Tests;

public sealed class ComponentLifecycleHubTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void Publish_WhenInactive_DoesNotRaiseLifecyclePublished()
    {
        var hub = new ComponentLifecycleHub();
        var raised = 0;
        hub.LifecyclePublished += _ => raised++;

        hub.Publish(CreateEvent());

        Assert.Equal(0, raised);
        Assert.Single(hub.GetSnapshot().RecentEvents);
    }

    [Fact]
    public void Publish_WhenActive_RaisesLifecyclePublished()
    {
        var hub = new ComponentLifecycleHub();
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
        var hub = new ComponentLifecycleHub();
        var lifecycleEvent = hub.CreateEvent(
            7,
            "Sample.App",
            ComponentLifecyclePhase.Initializing,
            parentComponentId: 3,
            source: "renderer");

        hub.Publish(lifecycleEvent);

        var snapshot = hub.GetSnapshot();

        Assert.Single(snapshot.Components);
        Assert.Equal(7, snapshot.Components[0].ComponentId);
        Assert.Equal(ComponentLifecyclePhase.Initializing, snapshot.Components[0].LastPhase);
        Assert.Single(snapshot.RecentEvents);
    }

    [Fact]
    public void CreateEvent_IncrementsSequence()
    {
        var hub = new ComponentLifecycleHub();

        var first = hub.CreateEvent(1, "A", ComponentLifecyclePhase.Rendering);
        var second = hub.CreateEvent(2, "B", ComponentLifecyclePhase.Rendering);

        Assert.Equal(first.Sequence + 1, second.Sequence);
    }

    [Fact]
    public void Publish_TrimsRecentEventsTo500()
    {
        var hub = new ComponentLifecycleHub();

        for (var i = 0; i < 501; i++)
        {
            hub.Publish(hub.CreateEvent(i, "T", ComponentLifecyclePhase.Rendering));
        }

        Assert.Equal(500, hub.GetSnapshot().RecentEvents.Count);
        Assert.Equal(1, hub.GetSnapshot().RecentEvents[0].ComponentId);
    }

    private ComponentLifecycleEvent CreateEvent() =>
        _fixture.Build<ComponentLifecycleEvent>()
            .With(e => e.Phase, ComponentLifecyclePhase.AfterRender)
            .Create();
}
