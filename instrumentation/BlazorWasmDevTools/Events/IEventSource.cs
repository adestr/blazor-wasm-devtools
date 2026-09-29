using BlazorWasmDevTools.Models;

namespace BlazorWasmDevTools.Events;

public interface IEventSource
{
    void SetActive(bool active);

    DevToolsComponentSnapshot GetSnapshot();

    event Action<ComponentLifecycleEvent>? LifecyclePublished;
}
