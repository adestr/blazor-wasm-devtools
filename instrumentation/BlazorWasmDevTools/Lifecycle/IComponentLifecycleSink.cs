using BlazorWasmDevTools.Models;

namespace BlazorWasmDevTools.Lifecycle;

public interface IComponentLifecycleSink
{
    void Publish(ComponentLifecycleEvent lifecycleEvent);
}
