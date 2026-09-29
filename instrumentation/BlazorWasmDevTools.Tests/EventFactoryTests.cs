using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Models;

namespace BlazorWasmDevTools.Tests;

public sealed class EventFactoryTests
{
    [Fact]
    public void Lifecycle_AssignsIncreasingSequences()
    {
        var first = EventFactory.Lifecycle(1, "A", ComponentLifecyclePhase.Rendering);
        var second = EventFactory.Lifecycle(2, "B", ComponentLifecyclePhase.Rendering);

        Assert.True(second.Sequence > first.Sequence);
    }

    [Fact]
    public void Lifecycle_WithParent_PopulatesFields()
    {
        var evt = EventFactory.Lifecycle(
            7,
            "Sample.App",
            parentComponentId: 3,
            phase: ComponentLifecyclePhase.Initializing,
            firstRender: true,
            source: "renderer");

        Assert.Equal(7, evt.ComponentId);
        Assert.Equal("Sample.App", evt.ComponentType);
        Assert.Equal(3, evt.ParentComponentId);
        Assert.Equal(ComponentLifecyclePhase.Initializing, evt.Phase);
        Assert.True(evt.FirstRender);
        Assert.Equal("renderer", evt.Source);
        Assert.True(evt.TimestampUnixMilliseconds > 0);
    }

    [Fact]
    public void Lifecycle_WithoutParent_DefaultsParentToNull()
    {
        var evt = EventFactory.Lifecycle(1, "A", ComponentLifecyclePhase.AfterRender);

        Assert.Null(evt.ParentComponentId);
        Assert.False(evt.FirstRender);
        Assert.Equal("instrumentation", evt.Source);
    }

    [Fact]
    public void Generic_PopulatesFields()
    {
        var properties = new Dictionary<string, object?> { ["key"] = "value" };

        var evt = EventFactory.Generic("Created", properties, source: "activator");

        Assert.Equal("Created", evt.Name);
        Assert.Same(properties, evt.Properties);
        Assert.Equal("activator", evt.Source);
        Assert.True(evt.Sequence > 0);
        Assert.True(evt.TimestampUnixMilliseconds > 0);
    }
}
