using BlazorWasmDevTools.Lifecycle;

namespace BlazorWasmDevTools.Tests;

public sealed class ComponentInstanceIdsTests
{
    [Fact]
    public void GetOrAssign_ReturnsStableIdForSameInstance()
    {
        var component = new object();

        var first = ComponentInstanceIds.GetOrAssign(component);
        var second = ComponentInstanceIds.GetOrAssign(component);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetOrAssign_ReturnsDifferentIdsForDifferentInstances()
    {
        var left = ComponentInstanceIds.GetOrAssign(new object());
        var right = ComponentInstanceIds.GetOrAssign(new object());

        Assert.NotEqual(left, right);
    }
}
